USE [HomeAffairsDb];
GO

/*
    Required for filtered indexes and other indexed objects.

    Explicitly setting these options makes this migration
    reliable under sqlcmd and Testcontainers.
*/

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

SET ANSI_PADDING ON;
GO

SET ANSI_WARNINGS ON;
GO

SET CONCAT_NULL_YIELDS_NULL ON;
GO

SET ARITHABORT ON;
GO

SET NUMERIC_ROUNDABORT OFF;
GO


/*
    ============================================================
    Document Versioning

    Existing documents become:

        LogicalDocumentId = DocumentId
        VersionNumber     = 1

    New versions receive:
        same LogicalDocumentId
        incremented VersionNumber

    Binary content is immutable.
    ============================================================
*/


/*
    ------------------------------------------------------------
    Add LogicalDocumentId when missing
    ------------------------------------------------------------
*/

IF COL_LENGTH(
    N'[documents].Documents',
    N'LogicalDocumentId'
) IS NULL
BEGIN
    ALTER TABLE [documents].Documents
    ADD LogicalDocumentId UNIQUEIDENTIFIER NULL;
END;
GO


/*
    ------------------------------------------------------------
    Add VersionNumber when missing
    ------------------------------------------------------------
*/

IF COL_LENGTH(
    N'[documents].Documents',
    N'VersionNumber'
) IS NULL
BEGIN
    ALTER TABLE [documents].Documents
    ADD VersionNumber INT NULL;
END;
GO


/*
    ------------------------------------------------------------
    Backfill existing 9A documents.

    Every pre-versioning Document is version 1 of its
    own logical document.
    ------------------------------------------------------------
*/

UPDATE [documents].Documents
SET
    LogicalDocumentId =
        COALESCE(
            LogicalDocumentId,
            DocumentId
        ),

    VersionNumber =
        COALESCE(
            VersionNumber,
            1
        )

WHERE LogicalDocumentId IS NULL
   OR VersionNumber IS NULL;
GO


/*
    ------------------------------------------------------------
    Make version columns mandatory.
    ------------------------------------------------------------
*/

ALTER TABLE [documents].Documents
ALTER COLUMN LogicalDocumentId
    UNIQUEIDENTIFIER NOT NULL;
GO

ALTER TABLE [documents].Documents
ALTER COLUMN VersionNumber
    INT NOT NULL;
GO


/*
    ------------------------------------------------------------
    Version number constraint
    ------------------------------------------------------------
*/

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id =
            OBJECT_ID(
                N'[documents].Documents'
            )
      AND name =
            N'CK_Documents_VersionNumber'
)
BEGIN
    ALTER TABLE [documents].Documents
    ADD CONSTRAINT
        CK_Documents_VersionNumber
        CHECK
        (
            VersionNumber > 0
        );
END;
GO


/*
    ------------------------------------------------------------
    Logical document + version uniqueness

    Example:

        Logical A
            Version 1
            Version 2
            Version 3

    There may never be two Version 2 rows for Logical A.
    ------------------------------------------------------------
*/

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id =
            OBJECT_ID(
                N'[documents].Documents'
            )
      AND name =
            N'UX_Documents_LogicalVersion'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX
        UX_Documents_LogicalVersion
    ON [documents].Documents
    (
        LogicalDocumentId,
        VersionNumber
    );
END;
GO


/*
    ------------------------------------------------------------
    Exactly one active version per logical document.

    This is a filtered index, which is why
    QUOTED_IDENTIFIER and the other SET options above
    are explicitly enabled.
    ------------------------------------------------------------
*/

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id =
            OBJECT_ID(
                N'[documents].Documents'
            )
      AND name =
            N'UX_Documents_ActiveLogical'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX
        UX_Documents_ActiveLogical
    ON [documents].Documents
    (
        LogicalDocumentId
    )
    WHERE IsActive = 1;
END;
GO


/*
    ============================================================
    Replace 9A Create procedure

    Every newly uploaded logical document begins as:

        LogicalDocumentId = DocumentId
        VersionNumber     = 1
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

    /*
        Applicant must own the Application.
    */

    IF NOT EXISTS
    (
        SELECT 1

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

          AND applicant.IsActive = 1
    )
    BEGIN;
        THROW 53101,
            'The application could not be found.',
            1;
    END;


    /*
        Defensive size validation.
    */

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

        LogicalDocumentId,

        VersionNumber,

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

        @DocumentId,

        1,

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
        document.DocumentId,

        document.LogicalDocumentId,

        document.VersionNumber,

        document.ApplicationId,

        document.DocumentType,

        document.FileName,

        document.ContentType,

        document.FileExtension,

        document.FileSizeBytes,

        document.ContentHash,

        document.UploadedByUserId,

        document.IsActive,

        document.CreatedAtUtc,

        document.UpdatedAtUtc,

        document.RowVersion

    FROM [documents].Documents
        AS document

    WHERE document.DocumentId =
        @DocumentId;
END;
GO


/*
    ============================================================
    Normal applicant document listing

    Returns only the active/latest version.
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

          AND applicant.IsActive = 1
    )
    BEGIN;
        THROW 53101,
            'The application could not be found.',
            1;
    END;


    SELECT
        document.DocumentId,

        document.LogicalDocumentId,

        document.VersionNumber,

        document.ApplicationId,

        document.DocumentType,

        document.FileName,

        document.ContentType,

        document.FileExtension,

        document.FileSizeBytes,

        document.ContentHash,

        document.UploadedByUserId,

        document.IsActive,

        document.CreatedAtUtc,

        document.UpdatedAtUtc,

        document.RowVersion

    FROM [documents].Documents
        AS document

    WHERE document.ApplicationId =
            @ApplicationId

      AND document.IsActive = 1

    ORDER BY
        document.CreatedAtUtc,

        document.DocumentId;
END;
GO


/*
    ============================================================
    Retrieve metadata for one document version.

    Historical DocumentIds are intentionally valid here,
    provided the requesting applicant owns the Application.

    This allows future version-history inspection.
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
        document.DocumentId,

        document.LogicalDocumentId,

        document.VersionNumber,

        document.ApplicationId,

        document.DocumentType,

        document.FileName,

        document.ContentType,

        document.FileExtension,

        document.FileSizeBytes,

        document.ContentHash,

        document.UploadedByUserId,

        document.IsActive,

        document.CreatedAtUtc,

        document.UpdatedAtUtc,

        document.RowVersion

    FROM [documents].Documents
        AS document

    INNER JOIN [applications].Applications
        AS application

        ON application.ApplicationId =
            document.ApplicationId

    INNER JOIN [citizens].Citizens
        AS applicant

        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE document.DocumentId =
            @DocumentId

      AND document.ApplicationId =
            @ApplicationId

      AND applicant.UserId =
            @UserId

      AND applicant.IsActive = 1;
END;
GO


/*
    ============================================================
    Content retrieval.

    Historical versions are also intentionally downloadable
    by their DocumentId when the applicant owns the Application.

    This lets evidence history remain auditable.
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
        document.DocumentId,

        document.FileName,

        document.ContentType,

        document.FileSizeBytes,

        document.ContentHash,

        document.Content

    FROM [documents].Documents
        AS document

    INNER JOIN [applications].Applications
        AS application

        ON application.ApplicationId =
            document.ApplicationId

    INNER JOIN [citizens].Citizens
        AS applicant

        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE document.DocumentId =
            @DocumentId

      AND document.ApplicationId =
            @ApplicationId

      AND applicant.UserId =
            @UserId

      AND applicant.IsActive = 1;
END;
GO


/*
    ============================================================
    Create immutable new version.

    The caller supplies the currently active DocumentId.

    v1:
        active = 1

    Upload replacement:

        v1 -> active = 0
        v2 -> active = 1

    Binary content belonging to v1 is never changed.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].Document_CreateVersionForApplicant

    @DocumentId UNIQUEIDENTIFIER,

    @NewDocumentId UNIQUEIDENTIFIER,

    @ApplicationId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER,

    @FileName NVARCHAR(255),

    @ContentType NVARCHAR(150),

    @FileExtension NVARCHAR(20),

    @FileSizeBytes BIGINT,

    @ContentHash NVARCHAR(64),

    @Content VARBINARY(MAX),

    @ExpectedRowVersion BINARY(8),

    @CreatedAtUtc DATETIME2(7)

AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;


    BEGIN TRY

        BEGIN TRANSACTION;


        DECLARE @LogicalDocumentId
            UNIQUEIDENTIFIER;

        DECLARE @DocumentType
            NVARCHAR(80);

        DECLARE @CurrentVersion
            INT;

        DECLARE @CurrentHash
            NVARCHAR(64);


        /*
            UPDLOCK + HOLDLOCK prevents concurrent
            callers from both attempting to create the
            same subsequent version number.
        */

        SELECT
            @LogicalDocumentId =
                document.LogicalDocumentId,

            @DocumentType =
                document.DocumentType,

            @CurrentVersion =
                document.VersionNumber,

            @CurrentHash =
                document.ContentHash

        FROM [documents].Documents
            AS document
            WITH (UPDLOCK, HOLDLOCK)

        INNER JOIN [applications].Applications
            AS application

            ON application.ApplicationId =
                document.ApplicationId

        INNER JOIN [citizens].Citizens
            AS applicant

            ON applicant.CitizenId =
                application.ApplicantCitizenId

        WHERE document.DocumentId =
                @DocumentId

          AND document.ApplicationId =
                @ApplicationId

          AND document.IsActive = 1

          AND applicant.UserId =
                @UserId

          AND applicant.IsActive = 1;


        IF @LogicalDocumentId IS NULL
        BEGIN;
            THROW 53201,
                'The active document could not be found.',
                1;
        END;


        /*
            A replacement containing exactly the same
            bytes is not a meaningful new version.
        */

        IF @CurrentHash =
            @ContentHash
        BEGIN;
            THROW 53202,
                'The uploaded content is identical to the latest version.',
                1;
        END;


        IF @FileSizeBytes <= 0
           OR @FileSizeBytes > 15728640
        BEGIN;
            THROW 53102,
                'The document size is not valid.',
                1;
        END;


        /*
            Deactivate current version using optimistic
            concurrency.
        */

        UPDATE [documents].Documents

        SET
            IsActive =
                0,

            UpdatedAtUtc =
                @CreatedAtUtc

        WHERE DocumentId =
                @DocumentId

          AND IsActive = 1

          AND RowVersion =
                @ExpectedRowVersion;


        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 53203,
                'The document was modified by another request.',
                1;
        END;


        /*
            Insert immutable subsequent version.
        */

        INSERT INTO [documents].Documents
        (
            DocumentId,

            LogicalDocumentId,

            VersionNumber,

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
            @NewDocumentId,

            @LogicalDocumentId,

            @CurrentVersion + 1,

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


        COMMIT TRANSACTION;


        SELECT
            document.DocumentId,

            document.LogicalDocumentId,

            document.VersionNumber,

            document.ApplicationId,

            document.DocumentType,

            document.FileName,

            document.ContentType,

            document.FileExtension,

            document.FileSizeBytes,

            document.ContentHash,

            document.UploadedByUserId,

            document.IsActive,

            document.CreatedAtUtc,

            document.UpdatedAtUtc,

            document.RowVersion

        FROM [documents].Documents
            AS document

        WHERE document.DocumentId =
            @NewDocumentId;

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
    Version history.

    @DocumentId can identify any version.

    The logical identifier is resolved first and then all
    versions are returned newest first.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].Document_GetVersionHistoryForApplicant

    @DocumentId UNIQUEIDENTIFIER,

    @ApplicationId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER

AS
BEGIN
    SET NOCOUNT ON;


    DECLARE @LogicalDocumentId
        UNIQUEIDENTIFIER;


    SELECT
        @LogicalDocumentId =
            document.LogicalDocumentId

    FROM [documents].Documents
        AS document

    INNER JOIN [applications].Applications
        AS application

        ON application.ApplicationId =
            document.ApplicationId

    INNER JOIN [citizens].Citizens
        AS applicant

        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE document.DocumentId =
            @DocumentId

      AND document.ApplicationId =
            @ApplicationId

      AND applicant.UserId =
            @UserId

      AND applicant.IsActive = 1;


    IF @LogicalDocumentId IS NULL
    BEGIN;
        THROW 53201,
            'The document could not be found.',
            1;
    END;


    SELECT
        document.DocumentId,

        document.LogicalDocumentId,

        document.VersionNumber,

        document.ApplicationId,

        document.DocumentType,

        document.FileName,

        document.ContentType,

        document.FileExtension,

        document.FileSizeBytes,

        document.ContentHash,

        document.UploadedByUserId,

        document.IsActive,

        document.CreatedAtUtc,

        document.UpdatedAtUtc,

        document.RowVersion

    FROM [documents].Documents
        AS document

    WHERE document.LogicalDocumentId =
        @LogicalDocumentId

    ORDER BY
        document.VersionNumber DESC;
END;
GO


/*
    ============================================================
    Soft-delete active/latest version.

    Historical data remains in SQL.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].Document_DeactivateForApplicant

    @DocumentId UNIQUEIDENTIFIER,

    @ApplicationId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER,

    @ExpectedRowVersion BINARY(8),

    @DeactivatedAtUtc DATETIME2(7)

AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;


    UPDATE document

    SET
        IsActive =
            0,

        UpdatedAtUtc =
            @DeactivatedAtUtc

    FROM [documents].Documents
        AS document

    INNER JOIN [applications].Applications
        AS application

        ON application.ApplicationId =
            document.ApplicationId

    INNER JOIN [citizens].Citizens
        AS applicant

        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE document.DocumentId =
            @DocumentId

      AND document.ApplicationId =
            @ApplicationId

      AND document.IsActive = 1

      AND document.RowVersion =
            @ExpectedRowVersion

      AND applicant.UserId =
            @UserId

      AND applicant.IsActive = 1;


    IF @@ROWCOUNT = 0
    BEGIN

        IF NOT EXISTS
        (
            SELECT 1

            FROM [documents].Documents
                AS document

            INNER JOIN [applications].Applications
                AS application

                ON application.ApplicationId =
                    document.ApplicationId

            INNER JOIN [citizens].Citizens
                AS applicant

                ON applicant.CitizenId =
                    application.ApplicantCitizenId

            WHERE document.DocumentId =
                    @DocumentId

              AND document.ApplicationId =
                    @ApplicationId

              AND document.IsActive = 1

              AND applicant.UserId =
                    @UserId

              AND applicant.IsActive = 1
        )
        BEGIN;
            THROW 53201,
                'The active document could not be found.',
                1;
        END;


        THROW 53203,
            'The document was modified by another request.',
            1;

    END;
END;
GO
