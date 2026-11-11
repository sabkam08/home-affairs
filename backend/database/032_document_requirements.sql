USE [HomeAffairsDb];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO


IF OBJECT_ID(
    N'[documents].DocumentRequirements',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [documents].DocumentRequirements
    (
        DocumentRequirementId UNIQUEIDENTIFIER NOT NULL,

        ApplicationType NVARCHAR(60) NOT NULL,

        DocumentType NVARCHAR(80) NOT NULL,

        DisplayName NVARCHAR(150) NOT NULL,

        Description NVARCHAR(500) NULL,

        IsRequired BIT NOT NULL,

        MinimumCount INT NOT NULL,

        IsActive BIT NOT NULL
            CONSTRAINT DF_DocumentRequirements_IsActive
            DEFAULT 1,

        CreatedAtUtc DATETIME2(7) NOT NULL,

        UpdatedAtUtc DATETIME2(7) NULL,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_DocumentRequirements
            PRIMARY KEY CLUSTERED (
                DocumentRequirementId
            ),

        CONSTRAINT CK_DocumentRequirements_MinimumCount
            CHECK (
                MinimumCount >= 0
            ),

        CONSTRAINT UQ_DocumentRequirements_TypeDocument
            UNIQUE
            (
                ApplicationType,
                DocumentType
            )
    );

    CREATE NONCLUSTERED INDEX
        IX_DocumentRequirements_ApplicationType
    ON [documents].DocumentRequirements
    (
        ApplicationType,
        IsActive
    );
END;
GO


/*
    ============================================================
    Baseline requirements

    Sections 10 and 11 will expand these service-specific rules.
    ============================================================
*/

DECLARE @NowUtc DATETIME2(7) =
    SYSUTCDATETIME();


DECLARE @Requirements TABLE
(
    ApplicationType NVARCHAR(60),
    DocumentType NVARCHAR(80),
    DisplayName NVARCHAR(150),
    Description NVARCHAR(500),
    MinimumCount INT
);


INSERT INTO @Requirements
VALUES

/*
    Birth Services
*/

(
    N'BIRTH_REGISTRATION',
    N'IDENTITY_DOCUMENT',
    N'Parent or Guardian Identity Document',
    N'Identity evidence for the applicant registering the birth.',
    1
),

(
    N'BIRTH_REGISTRATION',
    N'PROOF_OF_ADDRESS',
    N'Proof of Address',
    N'Current residential address evidence.',
    1
),

(
    N'BIRTH_CERTIFICATE_REISSUE',
    N'IDENTITY_DOCUMENT',
    N'Identity Document',
    N'Identity evidence for the certificate request.',
    1
),

(
    N'LATE_BIRTH_REGISTRATION',
    N'IDENTITY_DOCUMENT',
    N'Identity Document',
    N'Identity evidence supporting late registration.',
    1
),

(
    N'LATE_BIRTH_REGISTRATION',
    N'PROOF_OF_ADDRESS',
    N'Proof of Address',
    N'Current residential address evidence.',
    1
),

(
    N'LATE_BIRTH_REGISTRATION',
    N'AFFIDAVIT',
    N'Affidavit',
    N'Supporting affidavit for late birth registration.',
    1
),

(
    N'BIRTH_CERTIFICATE_AMENDMENT',
    N'IDENTITY_DOCUMENT',
    N'Identity Document',
    N'Identity evidence for the amendment request.',
    1
),

(
    N'BIRTH_CERTIFICATE_AMENDMENT',
    N'BIRTH_CERTIFICATE',
    N'Current Birth Certificate',
    N'The certificate being amended.',
    1
),


/*
    Smart ID Services
*/

(
    N'SMART_ID_NEW',
    N'IDENTITY_DOCUMENT',
    N'Identity Evidence',
    N'Identity evidence supporting the Smart ID application.',
    1
),

(
    N'SMART_ID_NEW',
    N'PROOF_OF_ADDRESS',
    N'Proof of Address',
    N'Current residential address evidence.',
    1
),

(
    N'SMART_ID_REPLACEMENT_LOST',
    N'IDENTITY_DOCUMENT',
    N'Identity Evidence',
    N'Identity evidence supporting replacement.',
    1
),

(
    N'SMART_ID_REPLACEMENT_LOST',
    N'PROOF_OF_ADDRESS',
    N'Proof of Address',
    N'Current residential address evidence.',
    1
),

(
    N'SMART_ID_REPLACEMENT_STOLEN',
    N'IDENTITY_DOCUMENT',
    N'Identity Evidence',
    N'Identity evidence supporting replacement.',
    1
),

(
    N'SMART_ID_REPLACEMENT_STOLEN',
    N'PROOF_OF_ADDRESS',
    N'Proof of Address',
    N'Current residential address evidence.',
    1
),

(
    N'SMART_ID_REPLACEMENT_STOLEN',
    N'AFFIDAVIT',
    N'Theft Affidavit',
    N'Affidavit supporting the stolen Smart ID replacement.',
    1
),

(
    N'SMART_ID_REPLACEMENT_DAMAGED',
    N'IDENTITY_DOCUMENT',
    N'Identity Evidence',
    N'Identity evidence supporting replacement.',
    1
),

(
    N'SMART_ID_REPLACEMENT_DAMAGED',
    N'PROOF_OF_ADDRESS',
    N'Proof of Address',
    N'Current residential address evidence.',
    1
),

(
    N'SMART_ID_CORRECTION',
    N'IDENTITY_DOCUMENT',
    N'Identity Evidence',
    N'Identity evidence supporting correction.',
    1
),

(
    N'SMART_ID_CORRECTION',
    N'SUPPORTING_DOCUMENT',
    N'Correction Supporting Document',
    N'Documentary evidence supporting the requested correction.',
    1
);


INSERT INTO [documents].DocumentRequirements
(
    DocumentRequirementId,
    ApplicationType,
    DocumentType,
    DisplayName,
    Description,
    IsRequired,
    MinimumCount,
    IsActive,
    CreatedAtUtc,
    UpdatedAtUtc
)
SELECT
    NEWID(),
    source.ApplicationType,
    source.DocumentType,
    source.DisplayName,
    source.Description,
    1,
    source.MinimumCount,
    1,
    @NowUtc,
    NULL

FROM @Requirements AS source

WHERE NOT EXISTS
(
    SELECT 1

    FROM [documents].DocumentRequirements
        AS existing

    WHERE existing.ApplicationType =
            source.ApplicationType

      AND existing.DocumentType =
            source.DocumentType
);
GO


/*
    ============================================================
    DocumentRequirement_GetByApplicationType
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].DocumentRequirement_GetByApplicationType

    @ApplicationType NVARCHAR(60)

AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        requirement.DocumentRequirementId,
        requirement.ApplicationType,
        requirement.DocumentType,
        requirement.DisplayName,
        requirement.Description,
        requirement.IsRequired,
        requirement.MinimumCount,
        requirement.IsActive,
        requirement.CreatedAtUtc,
        requirement.UpdatedAtUtc,
        requirement.RowVersion

    FROM [documents].DocumentRequirements
        AS requirement

    WHERE requirement.ApplicationType =
            @ApplicationType

      AND requirement.IsActive = 1

    ORDER BY
        requirement.DocumentType;
END;
GO


/*
    ============================================================
    Application document readiness.

    Only ACTIVE/latest document versions count.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [documents].DocumentRequirement_GetReadinessForApplicant

    @ApplicationId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER

AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ApplicationType NVARCHAR(60);


    SELECT
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


    IF @ApplicationType IS NULL
    BEGIN;
        THROW 53301,
            'The application could not be found.',
            1;
    END;


    SELECT
        requirement.DocumentRequirementId,

        requirement.ApplicationType,

        requirement.DocumentType,

        requirement.DisplayName,

        requirement.Description,

        requirement.IsRequired,

        requirement.MinimumCount,

        COUNT(document.DocumentId)
            AS UploadedCount,

        CAST(
            CASE
                WHEN COUNT(document.DocumentId) >=
                     requirement.MinimumCount
                THEN 1
                ELSE 0
            END
            AS BIT
        ) AS IsSatisfied

    FROM [documents].DocumentRequirements
        AS requirement

    LEFT JOIN [documents].Documents
        AS document

        ON document.ApplicationId =
            @ApplicationId

       AND document.DocumentType =
            requirement.DocumentType

       AND document.IsActive = 1

    WHERE requirement.ApplicationType =
            @ApplicationType

      AND requirement.IsActive = 1

      AND requirement.IsRequired = 1

    GROUP BY
        requirement.DocumentRequirementId,
        requirement.ApplicationType,
        requirement.DocumentType,
        requirement.DisplayName,
        requirement.Description,
        requirement.IsRequired,
        requirement.MinimumCount

    ORDER BY
        requirement.DocumentType;
END;
GO
