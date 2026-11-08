USE [HomeAffairsDb];
GO

/*
    ============================================================
    Application_UpdateDraft

    Only DRAFT applications may be modified.

    The applicant must own the application.

    Subject rules remain:
    - applicant may select themselves;
    - or an actively represented subject.

    Optimistic concurrency is enforced by RowVersion.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_UpdateDraft
    @ApplicationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @ApplicationType NVARCHAR(60),
    @SubjectCitizenId UNIQUEIDENTIFIER,
    @ExpectedRowVersion BINARY(8),
    @UpdatedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ApplicantCitizenId
        UNIQUEIDENTIFIER;

    DECLARE @CurrentStatus
        NVARCHAR(30);

    SELECT
        @ApplicantCitizenId =
            a.ApplicantCitizenId,

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

    IF @ApplicantCitizenId IS NULL
    BEGIN;
        THROW 52410,
            'The application could not be found.',
            1;
    END;

    IF @CurrentStatus <> N'DRAFT'
    BEGIN;
        THROW 52411,
            'Only draft applications may be updated.',
            1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [citizens].Citizens
        WHERE CitizenId =
            @SubjectCitizenId
          AND IsActive = 1
    )
    BEGIN;
        THROW 52402,
            'The subject citizen could not be found.',
            1;
    END;

    IF @SubjectCitizenId <>
        @ApplicantCitizenId
       AND NOT EXISTS
    (
        SELECT 1
        FROM [citizens].CitizenRelationships
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

    UPDATE [applications].Applications
    SET
        ApplicationType =
            @ApplicationType,

        SubjectCitizenId =
            @SubjectCitizenId,

        UpdatedAtUtc =
            @UpdatedAtUtc

    WHERE ApplicationId =
        @ApplicationId
      AND RowVersion =
        @ExpectedRowVersion
      AND Status = N'DRAFT';

    IF @@ROWCOUNT = 0
    BEGIN;
        THROW 52412,
            'The application was modified by another request.',
            1;
    END;

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
END;
GO


/*
    ============================================================
    Application_Submit

    Legal transition:
        DRAFT -> SUBMITTED

    Submission:
    - requires ownership;
    - requires current RowVersion;
    - sets SubmittedAtUtc;
    - records ApplicationStatusHistory atomically.
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
            THROW 52413,
                'Only draft applications may be submitted.',
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

    Legal transition:
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

        DECLARE @CurrentStatus
            NVARCHAR(30);

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
            COALESCE(
                NULLIF(
                    LTRIM(
                        RTRIM(@Reason)
                    ),
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
    Application_GetStatusHistoryForApplicant
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_GetStatusHistoryForApplicant
    @ApplicationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [applications].Applications AS a
        INNER JOIN [citizens].Citizens AS applicant
            ON applicant.CitizenId =
                a.ApplicantCitizenId
        WHERE a.ApplicationId =
            @ApplicationId
          AND applicant.UserId =
            @UserId
          AND applicant.IsActive = 1
    )
    BEGIN;
        THROW 52410,
            'The application could not be found.',
            1;
    END;

    SELECT
        h.ApplicationStatusHistoryId,
        h.ApplicationId,
        h.FromStatus,
        h.ToStatus,
        h.ChangedByUserId,
        h.Reason,
        h.ChangedAtUtc
    FROM [applications].ApplicationStatusHistory AS h
    WHERE h.ApplicationId =
        @ApplicationId
    ORDER BY
        h.ChangedAtUtc,
        h.ApplicationStatusHistoryId;
END;
GO
