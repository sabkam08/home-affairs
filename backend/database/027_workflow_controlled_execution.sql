USE [HomeAffairsDb];
GO


/*
    ============================================================
    Application_Submit

    DRAFT -> SUBMITTED

    Application.Status and WorkflowInstance.CurrentStateCode
    are changed within one SQL transaction.

    The pinned workflow definition must explicitly allow
    DRAFT -> SUBMITTED.
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

        DECLARE @CurrentStatus NVARCHAR(30);
        DECLARE @WorkflowInstanceId UNIQUEIDENTIFIER;
        DECLARE @WorkflowDefinitionId UNIQUEIDENTIFIER;

        SELECT
            @CurrentStatus = a.Status
        FROM [applications].Applications AS a
        INNER JOIN [citizens].Citizens AS applicant
            ON applicant.CitizenId =
                a.ApplicantCitizenId
        WHERE a.ApplicationId =
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

        SELECT
            @WorkflowInstanceId =
                wi.WorkflowInstanceId,

            @WorkflowDefinitionId =
                wi.WorkflowDefinitionId

        FROM [workflow].WorkflowInstances AS wi
            WITH (UPDLOCK, HOLDLOCK)

        WHERE wi.ApplicationId =
            @ApplicationId
          AND wi.CurrentStateCode =
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
            FROM [workflow].WorkflowTransitions AS wt
            WHERE wt.WorkflowDefinitionId =
                    @WorkflowDefinitionId
              AND wt.FromStateCode =
                    N'DRAFT'
              AND wt.ToStateCode =
                    N'SUBMITTED'
              AND wt.ActionCode =
                    N'SUBMIT'
              AND wt.IsActive = 1
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
        WHERE a.ApplicationId =
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


/*
    ============================================================
    Application_CancelDraft

    DRAFT -> CANCELLED
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_CancelDraft
    @ApplicationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @ExpectedRowVersion BINARY(8),
    @Reason NVARCHAR(500) = NULL,
    @CancelledAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(30);
        DECLARE @WorkflowInstanceId UNIQUEIDENTIFIER;
        DECLARE @WorkflowDefinitionId UNIQUEIDENTIFIER;

        SELECT
            @CurrentStatus =
                a.Status
        FROM [applications].Applications AS a
        INNER JOIN [citizens].Citizens AS applicant
            ON applicant.CitizenId =
                a.ApplicantCitizenId
        WHERE a.ApplicationId =
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
            THROW 52414,
                'Only draft applications may be cancelled by the applicant.',
                1;
        END;

        SELECT
            @WorkflowInstanceId =
                wi.WorkflowInstanceId,

            @WorkflowDefinitionId =
                wi.WorkflowDefinitionId

        FROM [workflow].WorkflowInstances AS wi
            WITH (UPDLOCK, HOLDLOCK)

        WHERE wi.ApplicationId =
                @ApplicationId
          AND wi.CurrentStateCode =
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
            FROM [workflow].WorkflowTransitions AS wt
            WHERE wt.WorkflowDefinitionId =
                    @WorkflowDefinitionId
              AND wt.FromStateCode =
                    N'DRAFT'
              AND wt.ToStateCode =
                    N'CANCELLED'
              AND wt.ActionCode =
                    N'CANCEL'
              AND wt.IsActive = 1
        )
        BEGIN;
            THROW 52902,
                'The workflow does not permit DRAFT to CANCELLED.',
                1;
        END;

        UPDATE [applications].Applications
        SET
            Status =
                N'CANCELLED',

            UpdatedAtUtc =
                @CancelledAtUtc

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
                N'CANCELLED',

            CompletedAtUtc =
                @CancelledAtUtc,

            UpdatedAtUtc =
                @CancelledAtUtc

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
            N'CANCELLED',
            @UserId,

            COALESCE
            (
                NULLIF(
                    LTRIM(RTRIM(@Reason)),
                    N''
                ),
                N'Application cancelled by applicant.'
            ),

            @CancelledAtUtc
        );

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
        WHERE a.ApplicationId =
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


/*
    ============================================================
    Application_Assign

    SUBMITTED -> IN_PROGRESS on initial assignment.

    Reassignment while already IN_PROGRESS:
    - changes assignment ownership;
    - does NOT perform another state transition;
    - leaves workflow at IN_PROGRESS.

    Assignment history remains preserved.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_Assign
    @ApplicationId UNIQUEIDENTIFIER,
    @AssignedToUserId UNIQUEIDENTIFIER,
    @AssignedByUserId UNIQUEIDENTIFIER,
    @Reason NVARCHAR(500) = NULL,
    @AssignedAtUtc DATETIME2(7),
    @ExpectedApplicationRowVersion BINARY(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Status NVARCHAR(30);
        DECLARE @WorkflowInstanceId UNIQUEIDENTIFIER;
        DECLARE @WorkflowDefinitionId UNIQUEIDENTIFIER;

        SELECT
            @Status =
                a.Status
        FROM [applications].Applications AS a
            WITH (UPDLOCK, HOLDLOCK)
        WHERE a.ApplicationId =
            @ApplicationId;

        IF @Status IS NULL
        BEGIN;
            THROW 52501,
                'The application could not be found.',
                1;
        END;

        IF @Status NOT IN
        (
            N'SUBMITTED',
            N'IN_PROGRESS'
        )
        BEGIN;
            THROW 52502,
                'The application is not assignable in its current state.',
                1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [identity].Users AS u
            INNER JOIN [identity].UserRoles AS ur
                ON ur.UserId =
                    u.UserId
            INNER JOIN [identity].Roles AS r
                ON r.RoleId =
                    ur.RoleId
            WHERE u.UserId =
                    @AssignedToUserId
              AND u.IsActive = 1
              AND r.Code IN
              (
                  N'PRACTITIONER',
                  N'SUPERVISOR'
              )
        )
        BEGIN;
            THROW 52503,
                'The target user is not an active practitioner or supervisor.',
                1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [applications].Applications
            WHERE ApplicationId =
                    @ApplicationId
              AND RowVersion =
                    @ExpectedApplicationRowVersion
        )
        BEGIN;
            THROW 52504,
                'The application was modified by another request.',
                1;
        END;

        SELECT
            @WorkflowInstanceId =
                wi.WorkflowInstanceId,

            @WorkflowDefinitionId =
                wi.WorkflowDefinitionId

        FROM [workflow].WorkflowInstances AS wi
            WITH (UPDLOCK, HOLDLOCK)

        WHERE wi.ApplicationId =
            @ApplicationId;

        IF @WorkflowInstanceId IS NULL
        BEGIN;
            THROW 52901,
                'The workflow instance could not be found.',
                1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [workflow].WorkflowInstances AS wi
            WHERE wi.WorkflowInstanceId =
                    @WorkflowInstanceId
              AND wi.CurrentStateCode =
                    @Status
        )
        BEGIN;
            THROW 52906,
                'Application and workflow states are not synchronized.',
                1;
        END;

        /*
            Only the first assignment performs:

                SUBMITTED -> IN_PROGRESS
        */
        IF @Status = N'SUBMITTED'
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM [workflow].WorkflowTransitions AS wt
                WHERE wt.WorkflowDefinitionId =
                        @WorkflowDefinitionId
                  AND wt.FromStateCode =
                        N'SUBMITTED'
                  AND wt.ToStateCode =
                        N'IN_PROGRESS'
                  AND wt.ActionCode =
                        N'ASSIGN'
                  AND wt.IsActive = 1
            )
            BEGIN;
                THROW 52902,
                    'The workflow does not permit assignment from SUBMITTED.',
                    1;
            END;
        END;

        /*
            Close old assignment if this is a reassignment.
        */
        UPDATE [applications].ApplicationAssignments
        SET
            IsActive =
                0,

            UnassignedAtUtc =
                @AssignedAtUtc,

            UnassignedByUserId =
                @AssignedByUserId

        WHERE ApplicationId =
                @ApplicationId
          AND IsActive = 1;

        INSERT INTO [applications].ApplicationAssignments
        (
            ApplicationAssignmentId,
            ApplicationId,
            AssignedToUserId,
            AssignedByUserId,
            AssignedAtUtc,
            UnassignedAtUtc,
            UnassignedByUserId,
            Reason,
            IsActive
        )
        VALUES
        (
            NEWID(),
            @ApplicationId,
            @AssignedToUserId,
            @AssignedByUserId,
            @AssignedAtUtc,
            NULL,
            NULL,
            @Reason,
            1
        );

        IF @Status = N'SUBMITTED'
        BEGIN
            UPDATE [applications].Applications
            SET
                Status =
                    N'IN_PROGRESS',

                UpdatedAtUtc =
                    @AssignedAtUtc

            WHERE ApplicationId =
                @ApplicationId;

            UPDATE [workflow].WorkflowInstances
            SET
                CurrentStateCode =
                    N'IN_PROGRESS',

                CompletedAtUtc =
                    NULL,

                UpdatedAtUtc =
                    @AssignedAtUtc

            WHERE WorkflowInstanceId =
                    @WorkflowInstanceId
              AND CurrentStateCode =
                    N'SUBMITTED';

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
                N'SUBMITTED',
                N'IN_PROGRESS',
                @AssignedByUserId,
                N'Application assigned for processing.',
                @AssignedAtUtc
            );
        END
        ELSE
        BEGIN
            /*
                Reassignment itself changes application
                RowVersion through UpdatedAtUtc, while the
                workflow state remains IN_PROGRESS.
            */
            UPDATE [applications].Applications
            SET
                UpdatedAtUtc =
                    @AssignedAtUtc
            WHERE ApplicationId =
                @ApplicationId;
        END;

        COMMIT TRANSACTION;

        SELECT
            aa.ApplicationAssignmentId,
            aa.ApplicationId,
            aa.AssignedToUserId,
            aa.AssignedByUserId,
            aa.AssignedAtUtc,
            aa.UnassignedAtUtc,
            aa.UnassignedByUserId,
            aa.Reason,
            aa.IsActive,
            aa.RowVersion
        FROM [applications].ApplicationAssignments AS aa
        WHERE aa.ApplicationId =
                @ApplicationId
          AND aa.IsActive = 1;
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


/*
    ============================================================
    Application_TransitionByAssignedPractitioner

    IN_PROGRESS -> AWAITING_INFORMATION
    IN_PROGRESS -> AWAITING_APPROVAL
    AWAITING_INFORMATION -> IN_PROGRESS

    Workflow definition is now authoritative for whether the
    transition exists.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_TransitionByAssignedPractitioner
    @ApplicationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @TargetStatus NVARCHAR(30),
    @Reason NVARCHAR(500) = NULL,
    @ExpectedRowVersion BINARY(8),
    @ChangedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentStatus NVARCHAR(30);
        DECLARE @WorkflowInstanceId UNIQUEIDENTIFIER;
        DECLARE @WorkflowDefinitionId UNIQUEIDENTIFIER;
        DECLARE @RequiresReason BIT;

        SELECT
            @CurrentStatus =
                a.Status
        FROM [applications].Applications AS a
        INNER JOIN [applications].ApplicationAssignments AS aa
            ON aa.ApplicationId =
                a.ApplicationId
        INNER JOIN [identity].Users AS u
            ON u.UserId =
                aa.AssignedToUserId
        WHERE a.ApplicationId =
                @ApplicationId
          AND aa.AssignedToUserId =
                @UserId
          AND aa.IsActive = 1
          AND u.IsActive = 1;

        IF @CurrentStatus IS NULL
        BEGIN;
            THROW 52601,
                'The assigned application could not be found.',
                1;
        END;

        SELECT
            @WorkflowInstanceId =
                wi.WorkflowInstanceId,

            @WorkflowDefinitionId =
                wi.WorkflowDefinitionId

        FROM [workflow].WorkflowInstances AS wi
            WITH (UPDLOCK, HOLDLOCK)

        WHERE wi.ApplicationId =
                @ApplicationId
          AND wi.CurrentStateCode =
                @CurrentStatus;

        IF @WorkflowInstanceId IS NULL
        BEGIN;
            THROW 52906,
                'Application and workflow states are not synchronized.',
                1;
        END;

        SELECT
            @RequiresReason =
                wt.RequiresReason

        FROM [workflow].WorkflowTransitions AS wt

        WHERE wt.WorkflowDefinitionId =
                @WorkflowDefinitionId
          AND wt.FromStateCode =
                @CurrentStatus
          AND wt.ToStateCode =
                @TargetStatus
          AND wt.ActionCode IN
          (
              N'REQUEST_INFORMATION',
              N'RESUME_PROCESSING',
              N'SEND_FOR_APPROVAL'
          )
          AND wt.IsActive = 1;

        IF @RequiresReason IS NULL
        BEGIN;
            THROW 52602,
                'The requested application status transition is not valid.',
                1;
        END;

        IF @RequiresReason = 1
           AND NULLIF(
               LTRIM(RTRIM(@Reason)),
               N''
           ) IS NULL
        BEGIN;
            THROW 52604,
                'A reason is required for this workflow transition.',
                1;
        END;

        UPDATE [applications].Applications
        SET
            Status =
                @TargetStatus,

            UpdatedAtUtc =
                @ChangedAtUtc

        WHERE ApplicationId =
                @ApplicationId
          AND Status =
                @CurrentStatus
          AND RowVersion =
                @ExpectedRowVersion;

        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 52603,
                'The application was modified by another request.',
                1;
        END;

        UPDATE [workflow].WorkflowInstances
        SET
            CurrentStateCode =
                @TargetStatus,

            CompletedAtUtc =
                NULL,

            UpdatedAtUtc =
                @ChangedAtUtc

        WHERE WorkflowInstanceId =
                @WorkflowInstanceId
          AND CurrentStateCode =
                @CurrentStatus;

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
            @CurrentStatus,
            @TargetStatus,
            @UserId,

            NULLIF(
                LTRIM(RTRIM(@Reason)),
                N''
            ),

            @ChangedAtUtc
        );

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
        WHERE a.ApplicationId =
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


/*
    ============================================================
    Application_Decide

    AWAITING_APPROVAL -> APPROVED
    AWAITING_APPROVAL -> REJECTED
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
        DECLARE @WorkflowInstanceId UNIQUEIDENTIFIER;
        DECLARE @WorkflowDefinitionId UNIQUEIDENTIFIER;
        DECLARE @RequiresReason BIT;

        SELECT
            @CurrentStatus =
                a.Status
        FROM [applications].Applications AS a
            WITH (UPDLOCK, HOLDLOCK)
        WHERE a.ApplicationId =
            @ApplicationId;

        IF @CurrentStatus IS NULL
        BEGIN;
            THROW 52701,
                'The application could not be found.',
                1;
        END;

        IF @CurrentStatus <>
            N'AWAITING_APPROVAL'
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

        IF NOT EXISTS
        (
            SELECT 1
            FROM [identity].Users AS u
            INNER JOIN [identity].UserRoles AS ur
                ON ur.UserId =
                    u.UserId
            INNER JOIN [identity].Roles AS r
                ON r.RoleId =
                    ur.RoleId
            WHERE u.UserId =
                    @DecisionByUserId
              AND u.IsActive = 1
              AND r.Code =
                    N'SUPERVISOR'
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
        WHERE aa.ApplicationId =
                @ApplicationId
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

        SELECT
            @WorkflowInstanceId =
                wi.WorkflowInstanceId,

            @WorkflowDefinitionId =
                wi.WorkflowDefinitionId

        FROM [workflow].WorkflowInstances AS wi
            WITH (UPDLOCK, HOLDLOCK)

        WHERE wi.ApplicationId =
                @ApplicationId
          AND wi.CurrentStateCode =
                N'AWAITING_APPROVAL';

        IF @WorkflowInstanceId IS NULL
        BEGIN;
            THROW 52906,
                'Application and workflow states are not synchronized.',
                1;
        END;

        SELECT
            @RequiresReason =
                wt.RequiresReason

        FROM [workflow].WorkflowTransitions AS wt

        WHERE wt.WorkflowDefinitionId =
                @WorkflowDefinitionId
          AND wt.FromStateCode =
                N'AWAITING_APPROVAL'
          AND wt.ToStateCode =
                @Decision
          AND wt.ActionCode =
                CASE
                    WHEN @Decision = N'APPROVED'
                    THEN N'APPROVE'
                    ELSE N'REJECT'
                END
          AND wt.IsActive = 1;

        IF @RequiresReason IS NULL
        BEGIN;
            THROW 52902,
                'The workflow does not permit the requested decision.',
                1;
        END;

        IF @RequiresReason = 1
           AND NULLIF(
               LTRIM(RTRIM(@Reason)),
               N''
           ) IS NULL
        BEGIN;
            THROW 52704,
                'A rejection reason is required.',
                1;
        END;

        UPDATE [applications].Applications
        SET
            Status =
                @Decision,

            UpdatedAtUtc =
                @DecidedAtUtc

        WHERE ApplicationId =
                @ApplicationId
          AND Status =
                N'AWAITING_APPROVAL'
          AND RowVersion =
                @ExpectedRowVersion;

        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 52708,
                'The application was modified by another request.',
                1;
        END;

        UPDATE [workflow].WorkflowInstances
        SET
            CurrentStateCode =
                @Decision,

            CompletedAtUtc =
                @DecidedAtUtc,

            UpdatedAtUtc =
                @DecidedAtUtc

        WHERE WorkflowInstanceId =
                @WorkflowInstanceId
          AND CurrentStateCode =
                N'AWAITING_APPROVAL';

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
            IsActive =
                0,

            UnassignedAtUtc =
                @DecidedAtUtc,

            UnassignedByUserId =
                @DecisionByUserId

        WHERE ApplicationId =
                @ApplicationId
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
        WHERE a.ApplicationId =
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
