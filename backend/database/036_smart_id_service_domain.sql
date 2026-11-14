USE [HomeAffairsDb];
GO

IF SCHEMA_ID(N'smartid') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [smartid]');
END;
GO


IF OBJECT_ID(
    N'[smartid].SmartIdServiceDetails',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE
        [smartid].SmartIdServiceDetails
    (
        SmartIdServiceDetailsId
            UNIQUEIDENTIFIER NOT NULL,

        ApplicationId
            UNIQUEIDENTIFIER NOT NULL,

        ExistingSmartIdNumber
            NVARCHAR(30) NULL,

        RequestedFirstName
            NVARCHAR(100) NULL,

        RequestedMiddleNames
            NVARCHAR(200) NULL,

        RequestedLastName
            NVARCHAR(100) NULL,

        RequestedDateOfBirth
            DATE NULL,

        RequestedSex
            CHAR(1) NULL,

        ReplacementReason
            NVARCHAR(1000) NULL,

        IncidentDate
            DATE NULL,

        PoliceCaseNumber
            NVARCHAR(100) NULL,

        PoliceStation
            NVARCHAR(200) NULL,

        DamageDescription
            NVARCHAR(1000) NULL,

        CorrectionField
            NVARCHAR(100) NULL,

        CurrentValue
            NVARCHAR(500) NULL,

        RequestedValue
            NVARCHAR(500) NULL,

        RequestReason
            NVARCHAR(1000) NULL,

        CreatedAtUtc
            DATETIME2(7) NOT NULL,

        UpdatedAtUtc
            DATETIME2(7) NULL,

        RowVersion
            ROWVERSION NOT NULL,

        CONSTRAINT
            PK_SmartIdServiceDetails
            PRIMARY KEY CLUSTERED
            (
                SmartIdServiceDetailsId
            ),

        CONSTRAINT
            FK_SmartIdServiceDetails_Application
            FOREIGN KEY
            (
                ApplicationId
            )
            REFERENCES [applications].Applications
            (
                ApplicationId
            ),

        CONSTRAINT
            UQ_SmartIdServiceDetails_Application
            UNIQUE
            (
                ApplicationId
            ),

        CONSTRAINT
            CK_SmartIdServiceDetails_RequestedSex
            CHECK
            (
                RequestedSex IS NULL
                OR RequestedSex IN
                (
                    'M',
                    'F',
                    'X'
                )
            )
    );
END;
GO


/*
    ============================================================
    Save Smart ID service details for the Applicant.

    Rules:
    - Applicant must own the Application.
    - Application must be a supported Smart ID type.
    - Application must remain DRAFT.
    - Existing detail updates require RowVersion.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [smartid].SmartIdServiceDetails_SaveForApplicant

    @SmartIdServiceDetailsId
        UNIQUEIDENTIFIER,

    @ApplicationId
        UNIQUEIDENTIFIER,

    @UserId
        UNIQUEIDENTIFIER,

    @ExistingSmartIdNumber
        NVARCHAR(30) = NULL,

    @RequestedFirstName
        NVARCHAR(100) = NULL,

    @RequestedMiddleNames
        NVARCHAR(200) = NULL,

    @RequestedLastName
        NVARCHAR(100) = NULL,

    @RequestedDateOfBirth
        DATE = NULL,

    @RequestedSex
        CHAR(1) = NULL,

    @ReplacementReason
        NVARCHAR(1000) = NULL,

    @IncidentDate
        DATE = NULL,

    @PoliceCaseNumber
        NVARCHAR(100) = NULL,

    @PoliceStation
        NVARCHAR(200) = NULL,

    @DamageDescription
        NVARCHAR(1000) = NULL,

    @CorrectionField
        NVARCHAR(100) = NULL,

    @CurrentValue
        NVARCHAR(500) = NULL,

    @RequestedValue
        NVARCHAR(500) = NULL,

    @RequestReason
        NVARCHAR(1000) = NULL,

    @ExpectedRowVersion
        BINARY(8) = NULL,

    @ChangedAtUtc
        DATETIME2(7)

AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;


    DECLARE @ApplicationType
        NVARCHAR(60);

    DECLARE @Status
        NVARCHAR(30);


    SELECT
        @ApplicationType =
            application.ApplicationType,

        @Status =
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


    IF @ApplicationType IS NULL
    BEGIN;
        THROW 53701,
            'The application could not be found.',
            1;
    END;


    IF @ApplicationType NOT IN
    (
        N'SMART_ID_NEW',
        N'SMART_ID_REPLACEMENT_LOST',
        N'SMART_ID_REPLACEMENT_STOLEN',
        N'SMART_ID_REPLACEMENT_DAMAGED',
        N'SMART_ID_CORRECTION'
    )
    BEGIN;
        THROW 53702,
            'The application is not a Smart ID service application.',
            1;
    END;


    IF @Status <> N'DRAFT'
    BEGIN;
        THROW 53703,
            'Smart ID service details may only be modified while the application is a draft.',
            1;
    END;


    IF EXISTS
    (
        SELECT 1

        FROM [smartid].SmartIdServiceDetails

        WHERE ApplicationId =
            @ApplicationId
    )
    BEGIN
        IF @ExpectedRowVersion IS NULL
        BEGIN;
            THROW 53704,
                'A row version is required when updating Smart ID service details.',
                1;
        END;


        UPDATE [smartid].SmartIdServiceDetails

        SET
            ExistingSmartIdNumber =
                @ExistingSmartIdNumber,

            RequestedFirstName =
                @RequestedFirstName,

            RequestedMiddleNames =
                @RequestedMiddleNames,

            RequestedLastName =
                @RequestedLastName,

            RequestedDateOfBirth =
                @RequestedDateOfBirth,

            RequestedSex =
                @RequestedSex,

            ReplacementReason =
                @ReplacementReason,

            IncidentDate =
                @IncidentDate,

            PoliceCaseNumber =
                @PoliceCaseNumber,

            PoliceStation =
                @PoliceStation,

            DamageDescription =
                @DamageDescription,

            CorrectionField =
                @CorrectionField,

            CurrentValue =
                @CurrentValue,

            RequestedValue =
                @RequestedValue,

            RequestReason =
                @RequestReason,

            UpdatedAtUtc =
                @ChangedAtUtc

        WHERE ApplicationId =
                @ApplicationId

          AND RowVersion =
                @ExpectedRowVersion;


        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 53705,
                'The Smart ID service details were modified by another request.',
                1;
        END;
    END
    ELSE
    BEGIN
        INSERT INTO
            [smartid].SmartIdServiceDetails
        (
            SmartIdServiceDetailsId,
            ApplicationId,
            ExistingSmartIdNumber,
            RequestedFirstName,
            RequestedMiddleNames,
            RequestedLastName,
            RequestedDateOfBirth,
            RequestedSex,
            ReplacementReason,
            IncidentDate,
            PoliceCaseNumber,
            PoliceStation,
            DamageDescription,
            CorrectionField,
            CurrentValue,
            RequestedValue,
            RequestReason,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            @SmartIdServiceDetailsId,
            @ApplicationId,
            @ExistingSmartIdNumber,
            @RequestedFirstName,
            @RequestedMiddleNames,
            @RequestedLastName,
            @RequestedDateOfBirth,
            @RequestedSex,
            @ReplacementReason,
            @IncidentDate,
            @PoliceCaseNumber,
            @PoliceStation,
            @DamageDescription,
            @CorrectionField,
            @CurrentValue,
            @RequestedValue,
            @RequestReason,
            @ChangedAtUtc,
            NULL
        );
    END;


    SELECT
        detail.SmartIdServiceDetailsId,

        detail.ApplicationId,

        application.ApplicationType,

        detail.ExistingSmartIdNumber,

        detail.RequestedFirstName,

        detail.RequestedMiddleNames,

        detail.RequestedLastName,

        detail.RequestedDateOfBirth,

        detail.RequestedSex,

        detail.ReplacementReason,

        detail.IncidentDate,

        detail.PoliceCaseNumber,

        detail.PoliceStation,

        detail.DamageDescription,

        detail.CorrectionField,

        detail.CurrentValue,

        detail.RequestedValue,

        detail.RequestReason,

        detail.CreatedAtUtc,

        detail.UpdatedAtUtc,

        detail.RowVersion

    FROM [smartid].SmartIdServiceDetails
        AS detail

    INNER JOIN [applications].Applications
        AS application

        ON application.ApplicationId =
            detail.ApplicationId

    WHERE detail.ApplicationId =
        @ApplicationId;
END;
GO


/*
    ============================================================
    Applicant retrieval.

    Ownership is intentionally part of the query so another
    Citizen receives no row and therefore HTTP 404.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [smartid].SmartIdServiceDetails_GetForApplicant

    @ApplicationId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER

AS
BEGIN
    SET NOCOUNT ON;


    SELECT
        detail.SmartIdServiceDetailsId,

        detail.ApplicationId,

        application.ApplicationType,

        detail.ExistingSmartIdNumber,

        detail.RequestedFirstName,

        detail.RequestedMiddleNames,

        detail.RequestedLastName,

        detail.RequestedDateOfBirth,

        detail.RequestedSex,

        detail.ReplacementReason,

        detail.IncidentDate,

        detail.PoliceCaseNumber,

        detail.PoliceStation,

        detail.DamageDescription,

        detail.CorrectionField,

        detail.CurrentValue,

        detail.RequestedValue,

        detail.RequestReason,

        detail.CreatedAtUtc,

        detail.UpdatedAtUtc,

        detail.RowVersion

    FROM [smartid].SmartIdServiceDetails
        AS detail

    INNER JOIN [applications].Applications
        AS application

        ON application.ApplicationId =
            detail.ApplicationId

    INNER JOIN [citizens].Citizens
        AS applicant

        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE detail.ApplicationId =
            @ApplicationId

      AND applicant.UserId =
            @UserId

      AND applicant.IsActive = 1;
END;
GO
