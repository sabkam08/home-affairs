USE [HomeAffairsDb];
GO

/*
    ============================================================
    Application_TransitionByAssignedPractitioner

    Supported practitioner transitions:

    IN_PROGRESS
        -> AWAITING_INFORMATION
        -> AWAITING_APPROVAL

    AWAITING_INFORMATION
        -> IN_PROGRESS

    Only the currently assigned practitioner may perform these
    transitions.

    Every transition:
    - checks ownership of the active assignment;
    - checks legal state transition;
    - checks RowVersion;
    - writes ApplicationStatusHistory.
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

        /*
            The application must be actively assigned to the
            authenticated practitioner.
        */
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

        /*
            Explicit transition matrix.
        */
        IF NOT
        (
            (
                @CurrentStatus = N'IN_PROGRESS'
                AND
                @TargetStatus IN
                (
                    N'AWAITING_INFORMATION',
                    N'AWAITING_APPROVAL'
                )
            )
            OR
            (
                @CurrentStatus = N'AWAITING_INFORMATION'
                AND
                @TargetStatus = N'IN_PROGRESS'
            )
        )
        BEGIN;
            THROW 52602,
                'The requested application status transition is not valid.',
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
                LTRIM(
                    RTRIM(@Reason)
                ),
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
    Application_GetAssignedById

    Staff-side retrieval.

    Only the current active assignee can retrieve the
    application through this endpoint.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_GetAssignedById
    @ApplicationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
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
    INNER JOIN [applications].ApplicationAssignments AS aa
        ON aa.ApplicationId =
            a.ApplicationId
    WHERE a.ApplicationId =
            @ApplicationId
      AND aa.AssignedToUserId =
            @UserId
      AND aa.IsActive = 1;
END;
GO
