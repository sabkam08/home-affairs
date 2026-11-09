USE [HomeAffairsDb];
GO

/*
    ============================================================
    Application Approval Queue

    Returns applications waiting for supervisor approval.
    Authorization remains enforced by the API permission policy.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_GetAwaitingApproval
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ApplicationId,
        a.ApplicationNumber,
        a.ApplicationType,
        a.ApplicantCitizenId,
        a.SubjectCitizenId,
        a.Status,
        a.SubmittedAtUtc,
        a.CreatedAtUtc,
        a.UpdatedAtUtc,
        a.RowVersion
    FROM [applications].Applications AS a
    WHERE a.Status = N'AWAITING_APPROVAL'
    ORDER BY
        a.UpdatedAtUtc,
        a.ApplicationId;
END;
GO


/*
    ============================================================
    Application_Decide

    Legal transitions:

        AWAITING_APPROVAL -> APPROVED
        AWAITING_APPROVAL -> REJECTED

    The decision-maker:
    - must be an active SUPERVISOR;
    - must not be the active practitioner assigned to the case;
    - must supply the current application RowVersion;
    - must provide a reason when rejecting.

    The active practitioner assignment is closed after the
    supervisor decision, while its history is retained.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_Decide
    @ApplicationId UNIQUEIDENTIFIER,
    @DecisionByUserId UNIQUEIDENTIFIER,
    @Decision NVARCHAR(30),
    @Reason NVARCHAR(500) = NULL,
    @ExpectedRowVersion BINARY(8),
    @DecidedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(30);
        DECLARE @AssignedPractitionerId UNIQUEIDENTIFIER;

        SELECT
            @CurrentStatus = a.Status
        FROM [applications].Applications AS a
            WITH (UPDLOCK, HOLDLOCK)
        WHERE a.ApplicationId = @ApplicationId;

        IF @CurrentStatus IS NULL
        BEGIN;
            THROW 52701,
                'The application could not be found.',
                1;
        END;

        IF @CurrentStatus <> N'AWAITING_APPROVAL'
        BEGIN;
            THROW 52702,
                'Only applications awaiting approval may be decided.',
                1;
        END;

        IF @Decision NOT IN
        (
            N'APPROVED',
            N'REJECTED'
        )
        BEGIN;
            THROW 52703,
                'The application decision is not valid.',
                1;
        END;

        IF @Decision = N'REJECTED'
           AND NULLIF(
               LTRIM(RTRIM(@Reason)),
               N''
           ) IS NULL
        BEGIN;
            THROW 52704,
                'A rejection reason is required.',
                1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [identity].Users AS u
            INNER JOIN [identity].UserRoles AS ur
                ON ur.UserId = u.UserId
            INNER JOIN [identity].Roles AS r
                ON r.RoleId = ur.RoleId
            WHERE u.UserId = @DecisionByUserId
              AND u.IsActive = 1
              AND r.Code = N'SUPERVISOR'
        )
        BEGIN;
            THROW 52705,
                'The decision-maker is not an active supervisor.',
                1;
        END;

        SELECT
            @AssignedPractitionerId =
                aa.AssignedToUserId
        FROM [applications].ApplicationAssignments AS aa
        WHERE aa.ApplicationId = @ApplicationId
          AND aa.IsActive = 1;

        IF @AssignedPractitionerId IS NULL
        BEGIN;
            THROW 52706,
                'The application does not have an active assignment.',
                1;
        END;

        IF @AssignedPractitionerId =
            @DecisionByUserId
        BEGIN;
            THROW 52707,
                'The assigned practitioner may not decide their own application.',
                1;
        END;

        UPDATE [applications].Applications
        SET
            Status = @Decision,
            UpdatedAtUtc = @DecidedAtUtc
        WHERE ApplicationId = @ApplicationId
          AND Status = N'AWAITING_APPROVAL'
          AND RowVersion = @ExpectedRowVersion;

        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 52708,
                'The application was modified by another request.',
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
            N'AWAITING_APPROVAL',
            @Decision,
            @DecisionByUserId,
            NULLIF(
                LTRIM(RTRIM(@Reason)),
                N''
            ),
            @DecidedAtUtc
        );

        UPDATE [applications].ApplicationAssignments
        SET
            IsActive = 0,
            UnassignedAtUtc = @DecidedAtUtc,
            UnassignedByUserId = @DecisionByUserId
        WHERE ApplicationId = @ApplicationId
          AND IsActive = 1;

        COMMIT TRANSACTION;

        SELECT
            a.ApplicationId,
            a.ApplicationNumber,
            a.ApplicationType,
            a.ApplicantCitizenId,
            a.SubjectCitizenId,
            a.Status,
            a.SubmittedAtUtc,
            a.CreatedAtUtc,
            a.UpdatedAtUtc,
            a.RowVersion
        FROM [applications].Applications AS a
        WHERE a.ApplicationId = @ApplicationId;
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
