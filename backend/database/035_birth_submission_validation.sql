USE [HomeAffairsDb];
GO

/*
    ============================================================
    BirthService_ValidateSubmission

    Called inside Application_Submit before any state is
    changed.

    Throws:
        53601 - Birth details do not exist.
        53602 - Birth details are incomplete.
        53603 - Required documents are incomplete.

    If this procedure succeeds, the Birth application is
    considered ready for submission.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [birth].BirthService_ValidateSubmission

    @ApplicationId UNIQUEIDENTIFIER

AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ApplicationType NVARCHAR(60);

    SELECT
        @ApplicationType =
            ApplicationType
    FROM [applications].Applications
    WHERE ApplicationId =
        @ApplicationId;


    /*
        This validator only applies to Birth services.
    */

    IF @ApplicationType NOT IN
    (
        N'BIRTH_REGISTRATION',
        N'BIRTH_CERTIFICATE_REISSUE',
        N'LATE_BIRTH_REGISTRATION',
        N'BIRTH_CERTIFICATE_AMENDMENT'
    )
    BEGIN
        RETURN;
    END;


    IF NOT EXISTS
    (
        SELECT 1
        FROM [birth].BirthServiceDetails
        WHERE ApplicationId =
            @ApplicationId
    )
    BEGIN;
        THROW 53601,
            'Birth service details are required before submission.',
            1;
    END;


    /*
        ========================================================
        BIRTH_REGISTRATION
        ========================================================
    */

    IF @ApplicationType =
        N'BIRTH_REGISTRATION'
       AND NOT EXISTS
    (
        SELECT 1
        FROM [birth].BirthServiceDetails
        WHERE ApplicationId =
                @ApplicationId

          AND NULLIF(
                LTRIM(RTRIM(ChildFirstName)),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(ChildLastName)),
                N''
              ) IS NOT NULL

          AND ChildDateOfBirth IS NOT NULL

          AND ChildSex IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(PlaceOfBirth)),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(CountryOfBirth)),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(MotherFirstName)),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(MotherLastName)),
                N''
              ) IS NOT NULL
    )
    BEGIN;
        THROW 53602,
            'Birth Registration details are incomplete.',
            1;
    END;


    /*
        ========================================================
        LATE_BIRTH_REGISTRATION
        ========================================================
    */

    IF @ApplicationType =
        N'LATE_BIRTH_REGISTRATION'
       AND NOT EXISTS
    (
        SELECT 1
        FROM [birth].BirthServiceDetails
        WHERE ApplicationId =
                @ApplicationId

          AND NULLIF(
                LTRIM(RTRIM(ChildFirstName)),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(ChildLastName)),
                N''
              ) IS NOT NULL

          AND ChildDateOfBirth IS NOT NULL

          AND ChildSex IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(PlaceOfBirth)),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(CountryOfBirth)),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(MotherFirstName)),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(MotherLastName)),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(RequestReason)),
                N''
              ) IS NOT NULL
    )
    BEGIN;
        THROW 53602,
            'Late Birth Registration details are incomplete.',
            1;
    END;


    /*
        ========================================================
        BIRTH_CERTIFICATE_REISSUE
        ========================================================
    */

    IF @ApplicationType =
        N'BIRTH_CERTIFICATE_REISSUE'
       AND NOT EXISTS
    (
        SELECT 1
        FROM [birth].BirthServiceDetails
        WHERE ApplicationId =
                @ApplicationId

          AND NULLIF(
                LTRIM(
                    RTRIM(
                        CurrentCertificateNumber
                    )
                ),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(RequestReason)),
                N''
              ) IS NOT NULL
    )
    BEGIN;
        THROW 53602,
            'Birth Certificate Re-Issue details are incomplete.',
            1;
    END;


    /*
        ========================================================
        BIRTH_CERTIFICATE_AMENDMENT
        ========================================================
    */

    IF @ApplicationType =
        N'BIRTH_CERTIFICATE_AMENDMENT'
       AND NOT EXISTS
    (
        SELECT 1
        FROM [birth].BirthServiceDetails
        WHERE ApplicationId =
                @ApplicationId

          AND NULLIF(
                LTRIM(
                    RTRIM(
                        CurrentCertificateNumber
                    )
                ),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(RequestReason)),
                N''
              ) IS NOT NULL
    )
    BEGIN;
        THROW 53602,
            'Birth Certificate Amendment details are incomplete.',
            1;
    END;


    /*
        ========================================================
        Required document validation

        Only active/latest Document versions count.

        For every required requirement:
            UploadedCount >= MinimumCount
        ========================================================
    */

    IF EXISTS
    (
        SELECT 1

        FROM [documents].DocumentRequirements
            AS requirement

        WHERE requirement.ApplicationType =
                @ApplicationType

          AND requirement.IsActive = 1

          AND requirement.IsRequired = 1

          AND
          (
              SELECT COUNT(1)

              FROM [documents].Documents
                  AS document

              WHERE document.ApplicationId =
                        @ApplicationId

                AND document.DocumentType =
                        requirement.DocumentType

                AND document.IsActive = 1

          ) < requirement.MinimumCount
    )
    BEGIN;
        THROW 53603,
            'One or more required documents are missing.',
            1;
    END;
END;
GO


/*
    ============================================================
    Replace Application_Submit

    Birth readiness is checked inside the same transaction
    BEFORE Application.Status or WorkflowInstance state changes.

    Non-Birth application types are unaffected by the Birth
    validation procedure.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_Submit

    @ApplicationId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER,

    @ExpectedRowVersion BINARY(8),

    @SubmittedAtUtc DATETIME2(7)

AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;


    BEGIN TRY

        BEGIN TRANSACTION;


        DECLARE @CurrentStatus
            NVARCHAR(30);

        DECLARE @ApplicationType
            NVARCHAR(60);

        DECLARE @WorkflowInstanceId
            UNIQUEIDENTIFIER;

        DECLARE @WorkflowDefinitionId
            UNIQUEIDENTIFIER;


        SELECT
            @CurrentStatus =
                application.Status,

            @ApplicationType =
                application.ApplicationType

        FROM [applications].Applications
            AS application

        INNER JOIN [citizens].Citizens
            AS applicant

            ON applicant.CitizenId =
                application.ApplicantCitizenId

        WHERE application.ApplicationId =
                @ApplicationId

          AND applicant.UserId =
                @UserId

          AND applicant.IsActive = 1;


        IF @CurrentStatus IS NULL
        BEGIN;
            THROW 52410,
                'The application could not be found.',
                1;
        END;


        IF @CurrentStatus <> N'DRAFT'
        BEGIN;
            THROW 52413,
                'Only draft applications may be submitted.',
                1;
        END;


        /*
            Birth-specific service readiness.

            The procedure simply returns for non-Birth
            applications.
        */

        EXEC [birth].BirthService_ValidateSubmission
            @ApplicationId =
                @ApplicationId;


        /*
            Workflow must already be synchronized.
        */

        SELECT
            @WorkflowInstanceId =
                instance.WorkflowInstanceId,

            @WorkflowDefinitionId =
                instance.WorkflowDefinitionId

        FROM [workflow].WorkflowInstances AS instance
            WITH (UPDLOCK, HOLDLOCK)

        WHERE instance.ApplicationId =
                @ApplicationId

          AND instance.CurrentStateCode =
                N'DRAFT';


        IF @WorkflowInstanceId IS NULL
        BEGIN;
            THROW 52901,
                'The workflow instance could not be found or is not synchronized.',
                1;
        END;


        IF NOT EXISTS
        (
            SELECT 1

            FROM [workflow].WorkflowTransitions
                AS transition

            WHERE transition.WorkflowDefinitionId =
                    @WorkflowDefinitionId

              AND transition.FromStateCode =
                    N'DRAFT'

              AND transition.ToStateCode =
                    N'SUBMITTED'

              AND transition.ActionCode =
                    N'SUBMIT'

              AND transition.IsActive = 1
        )
        BEGIN;
            THROW 52902,
                'The workflow does not permit DRAFT to SUBMITTED.',
                1;
        END;


        UPDATE [applications].Applications

        SET
            Status =
                N'SUBMITTED',

            SubmittedAtUtc =
                @SubmittedAtUtc,

            UpdatedAtUtc =
                @SubmittedAtUtc

        WHERE ApplicationId =
                @ApplicationId

          AND Status =
                N'DRAFT'

          AND RowVersion =
                @ExpectedRowVersion;


        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 52412,
                'The application was modified by another request.',
                1;
        END;


        UPDATE [workflow].WorkflowInstances

        SET
            CurrentStateCode =
                N'SUBMITTED',

            CompletedAtUtc =
                NULL,

            UpdatedAtUtc =
                @SubmittedAtUtc

        WHERE WorkflowInstanceId =
                @WorkflowInstanceId

          AND CurrentStateCode =
                N'DRAFT';


        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 52903,
                'The workflow instance was modified by another request.',
                1;
        END;


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
            N'DRAFT',
            N'SUBMITTED',
            @UserId,
            N'Application submitted by applicant.',
            @SubmittedAtUtc
        );


        COMMIT TRANSACTION;


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

        THROW;

    END CATCH;
END;
GO
