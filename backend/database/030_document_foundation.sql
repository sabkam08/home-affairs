USE [HomeAffairsDb];
GO

IF SCHEMA_ID(N'documents') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [documents]');
END;
GO


/*
    ============================================================
    Documents

    Binary content is stored directly in SQL Server using
    VARBINARY(MAX).

    Documents may belong to an Application and therefore
    indirectly to its Applicant / Subject.

    Versioning is introduced in 9B.
    ============================================================
*/

IF OBJECT_ID(
    N'[documents].Documents',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [documents].Documents
    (
        DocumentId UNIQUEIDENTIFIER NOT NULL,

        ApplicationId UNIQUEIDENTIFIER NOT NULL,

        DocumentType NVARCHAR(80) NOT NULL,

        FileName NVARCHAR(255) NOT NULL,

        ContentType NVARCHAR(150) NOT NULL,

        FileExtension NVARCHAR(20) NOT NULL,

        FileSizeBytes BIGINT NOT NULL,

        ContentHash NVARCHAR(64) NOT NULL,

        Content VARBINARY(MAX) NOT NULL,

        UploadedByUserId UNIQUEIDENTIFIER NOT NULL,

        IsActive BIT NOT NULL
            CONSTRAINT DF_Documents_IsActive
            DEFAULT 1,

        CreatedAtUtc DATETIME2(7) NOT NULL,

        UpdatedAtUtc DATETIME2(7) NULL,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_Documents
            PRIMARY KEY CLUSTERED (
                DocumentId
            ),

        CONSTRAINT FK_Documents_Application
            FOREIGN KEY (
                ApplicationId
            )
            REFERENCES [applications].Applications (
                ApplicationId
            ),

        CONSTRAINT FK_Documents_UploadedBy
            FOREIGN KEY (
                UploadedByUserId
            )
            REFERENCES [identity].Users (
                UserId
            ),

        CONSTRAINT CK_Documents_Type_NotEmpty
            CHECK
            (
                LEN(
                    LTRIM(
                        RTRIM(DocumentType)
                    )
                ) > 0
            ),

        CONSTRAINT CK_Documents_FileName_NotEmpty
            CHECK
            (
                LEN(
                    LTRIM(
                        RTRIM(FileName)
                    )
                ) > 0
            ),

        CONSTRAINT CK_Documents_FileSize
            CHECK
            (
                FileSizeBytes > 0
                AND
                FileSizeBytes <= 15728640
            ),

        CONSTRAINT CK_Documents_ContentHash
            CHECK
            (
                LEN(ContentHash) = 64
            )
    );

    CREATE NONCLUSTERED INDEX
        IX_Documents_Application
    ON [documents].Documents
    (
        ApplicationId,
        IsActive,
        CreatedAtUtc
    );

    CREATE NONCLUSTERED INDEX
        IX_Documents_Hash
    ON [documents].Documents
    (
        ContentHash
    );

    CREATE NONCLUSTERED INDEX
        IX_Documents_UploadedBy
    ON [documents].Documents
    (
        UploadedByUserId,
        CreatedAtUtc
    );
END;
GO


/*
    ============================================================
    Document_CreateForApplicant

    Applicant may upload to an application they own.

    Staff document operations are introduced separately.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].Document_CreateForApplicant
    @DocumentId UNIQUEIDENTIFIER,
    @ApplicationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @DocumentType NVARCHAR(80),
    @FileName NVARCHAR(255),
    @ContentType NVARCHAR(150),
    @FileExtension NVARCHAR(20),
    @FileSizeBytes BIGINT,
    @ContentHash NVARCHAR(64),
    @Content VARBINARY(MAX),
    @CreatedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [applications].Applications AS application
        INNER JOIN [citizens].Citizens AS applicant
            ON applicant.CitizenId =
                application.ApplicantCitizenId
        WHERE application.ApplicationId =
                @ApplicationId
          AND applicant.UserId =
                @UserId
          AND applicant.IsActive = 1
    )
    BEGIN;
        THROW 53101,
            'The application could not be found.',
            1;
    END;

    IF @FileSizeBytes <= 0
       OR @FileSizeBytes > 15728640
    BEGIN;
        THROW 53102,
            'The document size is not valid.',
            1;
    END;

    INSERT INTO [documents].Documents
    (
        DocumentId,
        ApplicationId,
        DocumentType,
        FileName,
        ContentType,
        FileExtension,
        FileSizeBytes,
        ContentHash,
        Content,
        UploadedByUserId,
        IsActive,
        CreatedAtUtc,
        UpdatedAtUtc
    )
    VALUES
    (
        @DocumentId,
        @ApplicationId,
        @DocumentType,
        @FileName,
        @ContentType,
        @FileExtension,
        @FileSizeBytes,
        @ContentHash,
        @Content,
        @UserId,
        1,
        @CreatedAtUtc,
        NULL
    );

    SELECT
        d.DocumentId,
        d.ApplicationId,
        d.DocumentType,
        d.FileName,
        d.ContentType,
        d.FileExtension,
        d.FileSizeBytes,
        d.ContentHash,
        d.UploadedByUserId,
        d.IsActive,
        d.CreatedAtUtc,
        d.UpdatedAtUtc,
        d.RowVersion
    FROM [documents].Documents AS d
    WHERE d.DocumentId =
        @DocumentId;
END;
GO


/*
    ============================================================
    Document_GetByApplicationForApplicant
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].Document_GetByApplicationForApplicant
    @ApplicationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [applications].Applications AS application
        INNER JOIN [citizens].Citizens AS applicant
            ON applicant.CitizenId =
                application.ApplicantCitizenId
        WHERE application.ApplicationId =
                @ApplicationId
          AND applicant.UserId =
                @UserId
          AND applicant.IsActive = 1
    )
    BEGIN;
        THROW 53101,
            'The application could not be found.',
            1;
    END;

    SELECT
        d.DocumentId,
        d.ApplicationId,
        d.DocumentType,
        d.FileName,
        d.ContentType,
        d.FileExtension,
        d.FileSizeBytes,
        d.ContentHash,
        d.UploadedByUserId,
        d.IsActive,
        d.CreatedAtUtc,
        d.UpdatedAtUtc,
        d.RowVersion
    FROM [documents].Documents AS d
    WHERE d.ApplicationId =
            @ApplicationId
      AND d.IsActive = 1
    ORDER BY
        d.CreatedAtUtc,
        d.DocumentId;
END;
GO


/*
    ============================================================
    Document_GetByIdForApplicant
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].Document_GetByIdForApplicant
    @DocumentId UNIQUEIDENTIFIER,
    @ApplicationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DocumentId,
        d.ApplicationId,
        d.DocumentType,
        d.FileName,
        d.ContentType,
        d.FileExtension,
        d.FileSizeBytes,
        d.ContentHash,
        d.UploadedByUserId,
        d.IsActive,
        d.CreatedAtUtc,
        d.UpdatedAtUtc,
        d.RowVersion
    FROM [documents].Documents AS d

    INNER JOIN [applications].Applications AS application
        ON application.ApplicationId =
            d.ApplicationId

    INNER JOIN [citizens].Citizens AS applicant
        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE d.DocumentId =
            @DocumentId
      AND d.ApplicationId =
            @ApplicationId
      AND applicant.UserId =
            @UserId
      AND applicant.IsActive = 1
      AND d.IsActive = 1;
END;
GO


/*
    ============================================================
    Document_GetContentForApplicant

    Binary content is deliberately retrieved only when the
    caller requests the file itself.

    Listing metadata therefore does not transfer VARBINARY data.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].Document_GetContentForApplicant
    @DocumentId UNIQUEIDENTIFIER,
    @ApplicationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DocumentId,
        d.FileName,
        d.ContentType,
        d.FileSizeBytes,
        d.ContentHash,
        d.Content
    FROM [documents].Documents AS d

    INNER JOIN [applications].Applications AS application
        ON application.ApplicationId =
            d.ApplicationId

    INNER JOIN [citizens].Citizens AS applicant
        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE d.DocumentId =
            @DocumentId
      AND d.ApplicationId =
            @ApplicationId
      AND applicant.UserId =
            @UserId
      AND applicant.IsActive = 1
      AND d.IsActive = 1;
END;
GO
