USE [HomeAffairsDb];
GO

IF SCHEMA_ID(N'applications') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [applications]');
END;
GO


/*
    ============================================================
    Applications

    ApplicantCitizenId:
        Citizen submitting/owning the application.

    SubjectCitizenId:
        Citizen for whom the service is being requested.

    These may be equal or different.
    ============================================================
*/

IF OBJECT_ID(
    N'[applications].Applications',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [applications].Applications
    (
        ApplicationId UNIQUEIDENTIFIER NOT NULL,

        ApplicationNumber NVARCHAR(30) NOT NULL,

        ApplicationType NVARCHAR(60) NOT NULL,

        ApplicantCitizenId UNIQUEIDENTIFIER NOT NULL,

        SubjectCitizenId UNIQUEIDENTIFIER NOT NULL,

        Status NVARCHAR(30) NOT NULL,

        SubmittedAtUtc DATETIME2(7) NULL,

        CreatedAtUtc DATETIME2(7) NOT NULL,

        UpdatedAtUtc DATETIME2(7) NULL,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_Applications
            PRIMARY KEY CLUSTERED (
                ApplicationId
            ),

        CONSTRAINT UQ_Applications_ApplicationNumber
            UNIQUE (
                ApplicationNumber
            ),

        CONSTRAINT FK_Applications_ApplicantCitizen
            FOREIGN KEY (
                ApplicantCitizenId
            )
            REFERENCES [citizens].Citizens (
                CitizenId
            ),

        CONSTRAINT FK_Applications_SubjectCitizen
            FOREIGN KEY (
                SubjectCitizenId
            )
            REFERENCES [citizens].Citizens (
                CitizenId
            ),

        CONSTRAINT CK_Applications_Status
            CHECK
            (
                Status IN
                (
                    N'DRAFT',
                    N'SUBMITTED',
                    N'IN_PROGRESS',
                    N'AWAITING_INFORMATION',
                    N'AWAITING_APPROVAL',
                    N'APPROVED',
                    N'REJECTED',
                    N'COMPLETED',
                    N'CANCELLED'
                )
            ),

        CONSTRAINT CK_Applications_Type_NotEmpty
            CHECK
            (
                LEN(
                    LTRIM(
                        RTRIM(ApplicationType)
                    )
                ) > 0
            )
    );

    CREATE NONCLUSTERED INDEX
        IX_Applications_Applicant
    ON [applications].Applications
    (
        ApplicantCitizenId,
        Status,
        CreatedAtUtc
    );

    CREATE NONCLUSTERED INDEX
        IX_Applications_Subject
    ON [applications].Applications
    (
        SubjectCitizenId,
        Status,
        CreatedAtUtc
    );

    CREATE NONCLUSTERED INDEX
        IX_Applications_Status
    ON [applications].Applications
    (
        Status,
        CreatedAtUtc
    );
END;
GO


/*
    ============================================================
    Application status history

    Every material application status transition will later be
    persisted here.

    Draft creation records the initial DRAFT state immediately.
    ============================================================
*/

IF OBJECT_ID(
    N'[applications].ApplicationStatusHistory',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE
        [applications].ApplicationStatusHistory
    (
        ApplicationStatusHistoryId
            UNIQUEIDENTIFIER NOT NULL,

        ApplicationId
            UNIQUEIDENTIFIER NOT NULL,

        FromStatus
            NVARCHAR(30) NULL,

        ToStatus
            NVARCHAR(30) NOT NULL,

        ChangedByUserId
            UNIQUEIDENTIFIER NULL,

        Reason
            NVARCHAR(500) NULL,

        ChangedAtUtc
            DATETIME2(7) NOT NULL,

        CONSTRAINT PK_ApplicationStatusHistory
            PRIMARY KEY CLUSTERED (
                ApplicationStatusHistoryId
            ),

        CONSTRAINT FK_ApplicationStatusHistory_Application
            FOREIGN KEY (
                ApplicationId
            )
            REFERENCES [applications].Applications (
                ApplicationId
            ),

        CONSTRAINT FK_ApplicationStatusHistory_User
            FOREIGN KEY (
                ChangedByUserId
            )
            REFERENCES [identity].Users (
                UserId
            )
    );

    CREATE NONCLUSTERED INDEX
        IX_ApplicationStatusHistory_Application
    ON [applications].ApplicationStatusHistory
    (
        ApplicationId,
        ChangedAtUtc
    );
END;
GO


/*
    ============================================================
    Application_CreateDraft

    Business rules enforced here:

    1. Applicant must be the active Citizen profile belonging
       to the authenticated User.

    2. Subject may be:
       - applicant themselves; or
       - an active represented Citizen connected by an active
         parent/guardian relationship.

    3. Application begins as DRAFT.

    4. Initial status-history entry is created atomically.
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
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [citizens].Citizens
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
        BEGIN;;
            THROW 52403,
                'The applicant is not authorised to act for the subject citizen.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [applications].Applications
            WHERE ApplicationNumber =
                @ApplicationNumber
        )
        BEGIN;
            THROW 52404,
                'The application number already exists.',
                1;
        END;

        INSERT INTO [applications].Applications
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
            N'DRAFT',
            NULL,
            @CreatedAtUtc,
            NULL
        );

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
            N'DRAFT',
            @UserId,
            N'Application draft created.',
            @CreatedAtUtc
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


CREATE OR ALTER PROCEDURE
    [applications].Application_GetByIdForApplicant
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
    INNER JOIN [citizens].Citizens AS applicant
        ON applicant.CitizenId =
            a.ApplicantCitizenId
    WHERE a.ApplicationId =
        @ApplicationId
      AND applicant.UserId =
        @UserId
      AND applicant.IsActive = 1;
END;
GO


CREATE OR ALTER PROCEDURE
    [applications].Application_GetMine
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
    INNER JOIN [citizens].Citizens AS applicant
        ON applicant.CitizenId =
            a.ApplicantCitizenId
    WHERE applicant.UserId =
        @UserId
      AND applicant.IsActive = 1
    ORDER BY
        a.CreatedAtUtc DESC,
        a.ApplicationId DESC;
END;
GO
