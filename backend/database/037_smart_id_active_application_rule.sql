USE [HomeAffairsDb];
GO

/*
    ============================================================
    Application_CreateDraft

    Adds concurrency-safe Smart ID exclusivity.

    Application-level transaction isolation is SERIALIZABLE
    while checking/creating Smart ID applications.

    This prevents two concurrent requests for the same Subject
    from both observing "no active application" and inserting.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_CreateDraft

    @ApplicationId UNIQUEIDENTIFIER,

    @ApplicationNumber NVARCHAR(30),

    @ApplicationType NVARCHAR(60),

    @ApplicantCitizenId UNIQUEIDENTIFIER,

    @SubjectCitizenId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER,

    @CreatedAtUtc DATETIME2(7)

AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY

        SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

        BEGIN TRANSACTION;


        /*
            Applicant ownership.
        */

        IF NOT EXISTS
        (
            SELECT 1

            FROM [citizens].Citizens
                WITH (HOLDLOCK)

            WHERE CitizenId =
                    @ApplicantCitizenId

              AND UserId =
                    @UserId

              AND IsActive = 1
        )
        BEGIN;
            THROW 52401,
                'The applicant citizen could not be found.',
                1;
        END;


        /*
            Subject existence.
        */

        IF NOT EXISTS
        (
            SELECT 1

            FROM [citizens].Citizens
                WITH (UPDLOCK, HOLDLOCK)

            WHERE CitizenId =
                    @SubjectCitizenId

              AND IsActive = 1
        )
        BEGIN;
            THROW 52402,
                'The subject citizen could not be found.',
                1;
        END;


        /*
            Applicant may act only for themselves or an active
            represented subject.
        */

        IF @SubjectCitizenId <>
            @ApplicantCitizenId
           AND NOT EXISTS
        (
            SELECT 1

            FROM [citizens].CitizenRelationships
                WITH (HOLDLOCK)

            WHERE RepresentativeCitizenId =
                    @ApplicantCitizenId

              AND SubjectCitizenId =
                    @SubjectCitizenId

              AND IsActive = 1
        )
        BEGIN;
            THROW 52403,
                'The applicant is not authorised to act for the subject citizen.',
                1;
        END;


        IF EXISTS
        (
            SELECT 1

            FROM [applications].Applications
                WITH (UPDLOCK, HOLDLOCK)

            WHERE ApplicationNumber =
                @ApplicationNumber
        )
        BEGIN;
            THROW 52404,
                'The application number already exists.',
                1;
        END;


        /*
            ====================================================
            Smart ID exclusivity

            Any Smart ID service shares the same active slot
            for the Subject Citizen.

            Locking the Subject Citizen row above serializes
            competing Smart ID application creation for the
            same subject.
            ====================================================
        */

        IF @ApplicationType IN
        (
            N'SMART_ID_NEW',
            N'SMART_ID_REPLACEMENT_LOST',
            N'SMART_ID_REPLACEMENT_STOLEN',
            N'SMART_ID_REPLACEMENT_DAMAGED',
            N'SMART_ID_CORRECTION'
        )
        BEGIN

            IF EXISTS
            (
                SELECT 1

                FROM [applications].Applications
                    WITH (UPDLOCK, HOLDLOCK)

                WHERE SubjectCitizenId =
                        @SubjectCitizenId

                  AND ApplicationType IN
                  (
                      N'SMART_ID_NEW',
                      N'SMART_ID_REPLACEMENT_LOST',
                      N'SMART_ID_REPLACEMENT_STOLEN',
                      N'SMART_ID_REPLACEMENT_DAMAGED',
                      N'SMART_ID_CORRECTION'
                  )

                  AND Status IN
                  (
                      N'DRAFT',
                      N'SUBMITTED',
                      N'IN_PROGRESS',
                      N'AWAITING_INFORMATION',
                      N'AWAITING_APPROVAL'
                  )
            )
            BEGIN;
                THROW 53801,
                    'The subject citizen already has an active Smart ID application.',
                    1;
            END;

        END;


        /*
            Resolve active workflow definition.
        */

        DECLARE @WorkflowDefinitionId
            UNIQUEIDENTIFIER;

        DECLARE @InitialStateCode
            NVARCHAR(30);


        SELECT
            @WorkflowDefinitionId =
                definition.WorkflowDefinitionId

        FROM [workflow].WorkflowDefinitions
            AS definition

        WHERE definition.ApplicationType =
                @ApplicationType

          AND definition.IsActive = 1;


        IF @WorkflowDefinitionId IS NULL
        BEGIN;
            THROW 52904,
                'No active workflow definition exists for this application type.',
                1;
        END;


        SELECT
            @InitialStateCode =
                state.StateCode

        FROM [workflow].WorkflowStates
            AS state

        WHERE state.WorkflowDefinitionId =
                @WorkflowDefinitionId

          AND state.IsInitial = 1;


        IF @InitialStateCode IS NULL
        BEGIN;
            THROW 52905,
                'The workflow definition does not contain an initial state.',
                1;
        END;


        /*
            Application.
        */

        INSERT INTO
            [applications].Applications
        (
            ApplicationId,
            ApplicationNumber,
            ApplicationType,
            ApplicantCitizenId,
            SubjectCitizenId,
            Status,
            SubmittedAtUtc,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            @ApplicationId,
            @ApplicationNumber,
            @ApplicationType,
            @ApplicantCitizenId,
            @SubjectCitizenId,
            @InitialStateCode,
            NULL,
            @CreatedAtUtc,
            NULL
        );


        /*
            Initial status history.
        */

        INSERT INTO
            [applications].ApplicationStatusHistory
        (
            ApplicationStatusHistoryId,
            ApplicationId,
            FromStatus,
            ToStatus,
            ChangedByUserId,
            Reason,
            ChangedAtUtc
        )
        VALUES
        (
            NEWID(),
            @ApplicationId,
            NULL,
            @InitialStateCode,
            @UserId,
            N'Application draft created.',
            @CreatedAtUtc
        );


        /*
            Workflow Instance is created atomically.
        */

        INSERT INTO
            [workflow].WorkflowInstances
        (
            WorkflowInstanceId,
            WorkflowDefinitionId,
            ApplicationId,
            CurrentStateCode,
            StartedAtUtc,
            CompletedAtUtc,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            NEWID(),
            @WorkflowDefinitionId,
            @ApplicationId,
            @InitialStateCode,
            @CreatedAtUtc,
            NULL,
            @CreatedAtUtc,
            NULL
        );


        COMMIT TRANSACTION;


        /*
            Reset the session isolation level because pooled
            SQL connections may later be reused.
        */

        SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


        SELECT
            application.ApplicationId,

            application.ApplicationNumber,

            application.ApplicationType,

            application.ApplicantCitizenId,

            application.SubjectCitizenId,

            application.Status,

            application.SubmittedAtUtc,

            application.CreatedAtUtc,

            application.UpdatedAtUtc,

            application.RowVersion

        FROM [applications].Applications
            AS application

        WHERE application.ApplicationId =
            @ApplicationId;

    END TRY

    BEGIN CATCH

        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        /*
            Also restore isolation on failure before the pooled
            connection is returned.
        */

        SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

        THROW;

    END CATCH;
END;
GO
