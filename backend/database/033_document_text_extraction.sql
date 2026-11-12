USE [HomeAffairsDb];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO


/*
    ============================================================
    DocumentTextExtractions

    One extraction record exists per immutable Document version.

    DOCX/PDF:
        text may be extracted directly.

    Images/scanned files:
        ExtractionStatus = OCR_REQUIRED

    OCR is handled asynchronously by HomeAffairs.Worker later.
    ============================================================
*/

IF OBJECT_ID(
    N'[documents].DocumentTextExtractions',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE
        [documents].DocumentTextExtractions
    (
        DocumentTextExtractionId
            UNIQUEIDENTIFIER NOT NULL,

        DocumentId
            UNIQUEIDENTIFIER NOT NULL,

        ApplicationId
            UNIQUEIDENTIFIER NOT NULL,

        ExtractionStatus
            NVARCHAR(30) NOT NULL,

        ExtractionMethod
            NVARCHAR(30) NULL,

        ExtractedText
            NVARCHAR(MAX) NULL,

        CharacterCount
            INT NOT NULL
            CONSTRAINT
                DF_DocumentTextExtractions_CharacterCount
            DEFAULT 0,

        ProcessedAtUtc
            DATETIME2(7) NULL,

        ErrorMessage
            NVARCHAR(1000) NULL,

        CreatedAtUtc
            DATETIME2(7) NOT NULL,

        UpdatedAtUtc
            DATETIME2(7) NULL,

        RowVersion
            ROWVERSION NOT NULL,

        CONSTRAINT
            PK_DocumentTextExtractions
            PRIMARY KEY CLUSTERED
            (
                DocumentTextExtractionId
            ),

        CONSTRAINT
            FK_DocumentTextExtractions_Document
            FOREIGN KEY
            (
                DocumentId
            )
            REFERENCES [documents].Documents
            (
                DocumentId
            ),

        CONSTRAINT
            FK_DocumentTextExtractions_Application
            FOREIGN KEY
            (
                ApplicationId
            )
            REFERENCES [applications].Applications
            (
                ApplicationId
            ),

        CONSTRAINT
            UQ_DocumentTextExtractions_Document
            UNIQUE
            (
                DocumentId
            ),

        CONSTRAINT
            CK_DocumentTextExtractions_Status
            CHECK
            (
                ExtractionStatus IN
                (
                    N'NOT_PROCESSED',
                    N'EXTRACTED',
                    N'OCR_REQUIRED',
                    N'OCR_COMPLETED',
                    N'FAILED'
                )
            ),

        CONSTRAINT
            CK_DocumentTextExtractions_Method
            CHECK
            (
                ExtractionMethod IS NULL
                OR
                ExtractionMethod IN
                (
                    N'DOCX',
                    N'PDF',
                    N'OCR'
                )
            ),

        CONSTRAINT
            CK_DocumentTextExtractions_CharacterCount
            CHECK
            (
                CharacterCount >= 0
            )
    );

    CREATE NONCLUSTERED INDEX
        IX_DocumentTextExtractions_Application
    ON [documents].DocumentTextExtractions
    (
        ApplicationId,
        ExtractionStatus
    );
END;
GO


/*
    ============================================================
    Save direct extraction result.

    Ownership is checked against the Application Applicant.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].DocumentText_SaveForApplicant

    @DocumentId UNIQUEIDENTIFIER,

    @ApplicationId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER,

    @ExtractionStatus NVARCHAR(30),

    @ExtractionMethod NVARCHAR(30) = NULL,

    @ExtractedText NVARCHAR(MAX) = NULL,

    @ProcessedAtUtc DATETIME2(7)

AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;


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

          AND applicant.UserId =
                @UserId

          AND applicant.IsActive = 1
    )
    BEGIN;
        THROW 53401,
            'The document could not be found.',
            1;
    END;


    DECLARE @CharacterCount INT =
        LEN(
            COALESCE(
                @ExtractedText,
                N''
            )
        );


    IF EXISTS
    (
        SELECT 1

        FROM [documents].DocumentTextExtractions

        WHERE DocumentId =
            @DocumentId
    )
    BEGIN
        UPDATE [documents].DocumentTextExtractions

        SET
            ExtractionStatus =
                @ExtractionStatus,

            ExtractionMethod =
                @ExtractionMethod,

            ExtractedText =
                @ExtractedText,

            CharacterCount =
                @CharacterCount,

            ProcessedAtUtc =
                @ProcessedAtUtc,

            ErrorMessage =
                NULL,

            UpdatedAtUtc =
                @ProcessedAtUtc

        WHERE DocumentId =
            @DocumentId;
    END
    ELSE
    BEGIN
        INSERT INTO
            [documents].DocumentTextExtractions
        (
            DocumentTextExtractionId,
            DocumentId,
            ApplicationId,
            ExtractionStatus,
            ExtractionMethod,
            ExtractedText,
            CharacterCount,
            ProcessedAtUtc,
            ErrorMessage,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            NEWID(),
            @DocumentId,
            @ApplicationId,
            @ExtractionStatus,
            @ExtractionMethod,
            @ExtractedText,
            @CharacterCount,
            @ProcessedAtUtc,
            NULL,
            @ProcessedAtUtc,
            NULL
        );
    END;


    SELECT
        extraction.DocumentTextExtractionId,

        extraction.DocumentId,

        extraction.ApplicationId,

        extraction.ExtractionStatus,

        extraction.ExtractionMethod,

        extraction.ExtractedText,

        extraction.CharacterCount,

        extraction.ProcessedAtUtc,

        extraction.ErrorMessage,

        extraction.CreatedAtUtc,

        extraction.UpdatedAtUtc,

        extraction.RowVersion

    FROM [documents].DocumentTextExtractions
        AS extraction

    WHERE extraction.DocumentId =
        @DocumentId;
END;
GO


/*
    ============================================================
    Get extraction for applicant.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].DocumentText_GetForApplicant

    @DocumentId UNIQUEIDENTIFIER,

    @ApplicationId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER

AS
BEGIN
    SET NOCOUNT ON;


    SELECT
        extraction.DocumentTextExtractionId,

        extraction.DocumentId,

        extraction.ApplicationId,

        extraction.ExtractionStatus,

        extraction.ExtractionMethod,

        extraction.ExtractedText,

        extraction.CharacterCount,

        extraction.ProcessedAtUtc,

        extraction.ErrorMessage,

        extraction.CreatedAtUtc,

        extraction.UpdatedAtUtc,

        extraction.RowVersion

    FROM [documents].DocumentTextExtractions
        AS extraction

    INNER JOIN [applications].Applications
        AS application

        ON application.ApplicationId =
            extraction.ApplicationId

    INNER JOIN [citizens].Citizens
        AS applicant

        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE extraction.DocumentId =
            @DocumentId

      AND extraction.ApplicationId =
            @ApplicationId

      AND applicant.UserId =
            @UserId

      AND applicant.IsActive = 1;
END;
GO


/*
    ============================================================
    Text search within one Applicant-owned Application.

    Only latest/active document versions appear in normal
    search results.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].DocumentText_SearchForApplicant

    @ApplicationId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER,

    @SearchText NVARCHAR(500)

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
        THROW 53402,
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

        extraction.ExtractionStatus,

        extraction.ExtractionMethod,

        extraction.CharacterCount,

        extraction.ProcessedAtUtc

    FROM [documents].Documents
        AS document

    INNER JOIN [documents].DocumentTextExtractions
        AS extraction

        ON extraction.DocumentId =
            document.DocumentId

    WHERE document.ApplicationId =
            @ApplicationId

      AND document.IsActive = 1

      AND extraction.ExtractionStatus IN
      (
          N'EXTRACTED',
          N'OCR_COMPLETED'
      )

      AND extraction.ExtractedText LIKE
          N'%' + @SearchText + N'%'

    ORDER BY
        document.CreatedAtUtc DESC,
        document.DocumentId DESC;
END;
GO
