USE [HomeAffairsDb];
GO

/*
    ============================================================
    SmartIdService_ValidateSubmission

    Throws:
        53901 - service details missing
        53902 - service details incomplete
        53903 - required documents incomplete

    Returns normally for non-Smart-ID applications.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [smartid].SmartIdService_ValidateSubmission

    @ApplicationId UNIQUEIDENTIFIER

AS
BEGIN
    SET NOCOUNT ON;


    DECLARE @ApplicationType
        NVARCHAR(60);


    SELECT
        @ApplicationType =
            ApplicationType

    FROM [applications].Applications

    WHERE ApplicationId =
        @ApplicationId;


    /*
        Validator is deliberately a no-op for non-Smart-ID
        services so it can safely be called by generic submit.
    */

    IF @ApplicationType NOT IN
    (
        N'SMART_ID_NEW',
        N'SMART_ID_REPLACEMENT_LOST',
        N'SMART_ID_REPLACEMENT_STOLEN',
        N'SMART_ID_REPLACEMENT_DAMAGED',
        N'SMART_ID_CORRECTION'
    )
    BEGIN
        RETURN;
    END;


    IF NOT EXISTS
    (
        SELECT 1

        FROM [smartid].SmartIdServiceDetails

        WHERE ApplicationId =
            @ApplicationId
    )
    BEGIN;
        THROW 53901,
            'Smart ID service details are required before submission.',
            1;
    END;


    /*
        ========================================================
        SMART_ID_NEW
        ========================================================
    */

    IF @ApplicationType =
        N'SMART_ID_NEW'
       AND NOT EXISTS
    (
        SELECT 1

        FROM [smartid].SmartIdServiceDetails

        WHERE ApplicationId =
                @ApplicationId

          AND NULLIF(
                LTRIM(RTRIM(RequestedFirstName)),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(RTRIM(RequestedLastName)),
                N''
              ) IS NOT NULL

          AND RequestedDateOfBirth IS NOT NULL

          AND RequestedSex IS NOT NULL
    )
    BEGIN;
        THROW 53902,
            'New Smart ID application details are incomplete.',
            1;
    END;


    /*
        ========================================================
        SMART_ID_REPLACEMENT_LOST
        ========================================================
    */

    IF @ApplicationType =
        N'SMART_ID_REPLACEMENT_LOST'
       AND NOT EXISTS
    (
        SELECT 1

        FROM [smartid].SmartIdServiceDetails

        WHERE ApplicationId =
                @ApplicationId

          AND NULLIF(
                LTRIM(
                    RTRIM(ExistingSmartIdNumber)
                ),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(
                    RTRIM(ReplacementReason)
                ),
                N''
              ) IS NOT NULL
    )
    BEGIN;
        THROW 53902,
            'Lost Smart ID replacement details are incomplete.',
            1;
    END;


    /*
        ========================================================
        SMART_ID_REPLACEMENT_STOLEN
        ========================================================
    */

    IF @ApplicationType =
        N'SMART_ID_REPLACEMENT_STOLEN'
       AND NOT EXISTS
    (
        SELECT 1

        FROM [smartid].SmartIdServiceDetails

        WHERE ApplicationId =
                @ApplicationId

          AND NULLIF(
                LTRIM(
                    RTRIM(ExistingSmartIdNumber)
                ),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(
                    RTRIM(ReplacementReason)
                ),
                N''
              ) IS NOT NULL

          AND IncidentDate IS NOT NULL

          AND NULLIF(
                LTRIM(
                    RTRIM(PoliceCaseNumber)
                ),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(
                    RTRIM(PoliceStation)
                ),
                N''
              ) IS NOT NULL
    )
    BEGIN;
        THROW 53902,
            'Stolen Smart ID replacement details are incomplete.',
            1;
    END;


    /*
        ========================================================
        SMART_ID_REPLACEMENT_DAMAGED
        ========================================================
    */

    IF @ApplicationType =
        N'SMART_ID_REPLACEMENT_DAMAGED'
       AND NOT EXISTS
    (
        SELECT 1

        FROM [smartid].SmartIdServiceDetails

        WHERE ApplicationId =
                @ApplicationId

          AND NULLIF(
                LTRIM(
                    RTRIM(ExistingSmartIdNumber)
                ),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(
                    RTRIM(DamageDescription)
                ),
                N''
              ) IS NOT NULL
    )
    BEGIN;
        THROW 53902,
            'Damaged Smart ID replacement details are incomplete.',
            1;
    END;


    /*
        ========================================================
        SMART_ID_CORRECTION
        ========================================================
    */

    IF @ApplicationType =
        N'SMART_ID_CORRECTION'
       AND NOT EXISTS
    (
        SELECT 1

        FROM [smartid].SmartIdServiceDetails

        WHERE ApplicationId =
                @ApplicationId

          AND NULLIF(
                LTRIM(
                    RTRIM(ExistingSmartIdNumber)
                ),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(
                    RTRIM(CorrectionField)
                ),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(
                    RTRIM(CurrentValue)
                ),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(
                    RTRIM(RequestedValue)
                ),
                N''
              ) IS NOT NULL

          AND NULLIF(
                LTRIM(
                    RTRIM(RequestReason)
                ),
                N''
              ) IS NOT NULL
    )
    BEGIN;
        THROW 53902,
            'Smart ID correction details are incomplete.',
            1;
    END;


    /*
        ========================================================
        Document readiness

        Requirements come from our existing
        [documents].DocumentRequirements table.

        Only active/latest versions count.
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
          )
          <
          requirement.MinimumCount
    )
    BEGIN;
        THROW 53903,
            'One or more required Smart ID documents are missing.',
            1;
    END;
END;
GO


/*
    ============================================================
    Replace generic Application_Submit.

    Validation order:

        1. Applicant ownership
        2. DRAFT state
        3. Birth validation
        4. Smart ID validation
        5. Workflow transition
        6. Application + Workflow state change
        7. Status history

    Everything remains one SQL transaction.
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

        DECLARE @WorkflowInstanceId
            UNIQUEIDENTIFIER;

        DECLARE @WorkflowDefinitionId
            UNIQUEIDENTIFIER;


        SELECT
            @CurrentStatus =
                application.Status

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
            Existing Birth rules.
            No-op for non-Birth applications.
        */

        EXEC [birth].BirthService_ValidateSubmission
            @ApplicationId =
                @ApplicationId;


        /*
            New Smart ID rules.
            No-op for non-Smart-ID applications.
        */

        EXEC [smartid].SmartIdService_ValidateSubmission
            @ApplicationId =
                @ApplicationId;


        SELECT
            @WorkflowInstanceId =
                instance.WorkflowInstanceId,

            @WorkflowDefinitionId =
                instance.WorkflowDefinitionId

        FROM [workflow].WorkflowInstances
            AS instance
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
