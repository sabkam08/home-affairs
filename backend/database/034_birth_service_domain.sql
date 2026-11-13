USE [HomeAffairsDb];
GO

IF SCHEMA_ID(N'birth') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [birth]');
END;
GO


IF OBJECT_ID(
    N'[birth].BirthServiceDetails',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [birth].BirthServiceDetails
    (
        BirthServiceDetailsId
            UNIQUEIDENTIFIER NOT NULL,

        ApplicationId
            UNIQUEIDENTIFIER NOT NULL,

        ChildFirstName
            NVARCHAR(100) NULL,

        ChildMiddleNames
            NVARCHAR(200) NULL,

        ChildLastName
            NVARCHAR(100) NULL,

        ChildDateOfBirth
            DATE NULL,

        ChildSex
            CHAR(1) NULL,

        PlaceOfBirth
            NVARCHAR(200) NULL,

        ProvinceOfBirth
            NVARCHAR(100) NULL,

        CountryOfBirth
            NVARCHAR(100) NULL,

        MotherFirstName
            NVARCHAR(100) NULL,

        MotherLastName
            NVARCHAR(100) NULL,

        MotherIdNumber
            NVARCHAR(30) NULL,

        FatherFirstName
            NVARCHAR(100) NULL,

        FatherLastName
            NVARCHAR(100) NULL,

        FatherIdNumber
            NVARCHAR(30) NULL,

        CurrentCertificateNumber
            NVARCHAR(100) NULL,

        RequestReason
            NVARCHAR(1000) NULL,

        CreatedAtUtc
            DATETIME2(7) NOT NULL,

        UpdatedAtUtc
            DATETIME2(7) NULL,

        RowVersion
            ROWVERSION NOT NULL,

        CONSTRAINT
            PK_BirthServiceDetails
            PRIMARY KEY CLUSTERED
            (
                BirthServiceDetailsId
            ),

        CONSTRAINT
            FK_BirthServiceDetails_Application
            FOREIGN KEY
            (
                ApplicationId
            )
            REFERENCES [applications].Applications
            (
                ApplicationId
            ),

        CONSTRAINT
            UQ_BirthServiceDetails_Application
            UNIQUE
            (
                ApplicationId
            ),

        CONSTRAINT
            CK_BirthServiceDetails_ChildSex
            CHECK
            (
                ChildSex IS NULL
                OR ChildSex IN
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
    Create / update Birth details.

    Only the Application owner may modify these details.

    Only Birth application types are accepted.

    Birth details remain editable while the Application is DRAFT.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [birth].BirthServiceDetails_SaveForApplicant

    @BirthServiceDetailsId
        UNIQUEIDENTIFIER,

    @ApplicationId
        UNIQUEIDENTIFIER,

    @UserId
        UNIQUEIDENTIFIER,

    @ChildFirstName
        NVARCHAR(100) = NULL,

    @ChildMiddleNames
        NVARCHAR(200) = NULL,

    @ChildLastName
        NVARCHAR(100) = NULL,

    @ChildDateOfBirth
        DATE = NULL,

    @ChildSex
        CHAR(1) = NULL,

    @PlaceOfBirth
        NVARCHAR(200) = NULL,

    @ProvinceOfBirth
        NVARCHAR(100) = NULL,

    @CountryOfBirth
        NVARCHAR(100) = NULL,

    @MotherFirstName
        NVARCHAR(100) = NULL,

    @MotherLastName
        NVARCHAR(100) = NULL,

    @MotherIdNumber
        NVARCHAR(30) = NULL,

    @FatherFirstName
        NVARCHAR(100) = NULL,

    @FatherLastName
        NVARCHAR(100) = NULL,

    @FatherIdNumber
        NVARCHAR(30) = NULL,

    @CurrentCertificateNumber
        NVARCHAR(100) = NULL,

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

    FROM [applications].Applications AS application

    INNER JOIN [citizens].Citizens AS applicant
        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE application.ApplicationId =
            @ApplicationId

      AND applicant.UserId =
            @UserId

      AND applicant.IsActive = 1;


    IF @ApplicationType IS NULL
    BEGIN;
        THROW 53501,
            'The application could not be found.',
            1;
    END;


    IF @ApplicationType NOT IN
    (
        N'BIRTH_REGISTRATION',
        N'BIRTH_CERTIFICATE_REISSUE',
        N'LATE_BIRTH_REGISTRATION',
        N'BIRTH_CERTIFICATE_AMENDMENT'
    )
    BEGIN;
        THROW 53502,
            'The application is not a Birth service application.',
            1;
    END;


    IF @Status <> N'DRAFT'
    BEGIN;
        THROW 53503,
            'Birth service details may only be modified while the application is a draft.',
            1;
    END;


    IF EXISTS
    (
        SELECT 1
        FROM [birth].BirthServiceDetails
        WHERE ApplicationId =
            @ApplicationId
    )
    BEGIN;
        IF @ExpectedRowVersion IS NULL
        BEGIN;
            THROW 53504,
                'A row version is required when updating Birth service details.',
                1;
        END;


        UPDATE [birth].BirthServiceDetails
        SET
            ChildFirstName =
                @ChildFirstName,

            ChildMiddleNames =
                @ChildMiddleNames,

            ChildLastName =
                @ChildLastName,

            ChildDateOfBirth =
                @ChildDateOfBirth,

            ChildSex =
                @ChildSex,

            PlaceOfBirth =
                @PlaceOfBirth,

            ProvinceOfBirth =
                @ProvinceOfBirth,

            CountryOfBirth =
                @CountryOfBirth,

            MotherFirstName =
                @MotherFirstName,

            MotherLastName =
                @MotherLastName,

            MotherIdNumber =
                @MotherIdNumber,

            FatherFirstName =
                @FatherFirstName,

            FatherLastName =
                @FatherLastName,

            FatherIdNumber =
                @FatherIdNumber,

            CurrentCertificateNumber =
                @CurrentCertificateNumber,

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
            THROW 53505,
                'The Birth service details were modified by another request.',
                1;
        END;
    END
    ELSE
    BEGIN
        INSERT INTO [birth].BirthServiceDetails
        (
            BirthServiceDetailsId,
            ApplicationId,
            ChildFirstName,
            ChildMiddleNames,
            ChildLastName,
            ChildDateOfBirth,
            ChildSex,
            PlaceOfBirth,
            ProvinceOfBirth,
            CountryOfBirth,
            MotherFirstName,
            MotherLastName,
            MotherIdNumber,
            FatherFirstName,
            FatherLastName,
            FatherIdNumber,
            CurrentCertificateNumber,
            RequestReason,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            @BirthServiceDetailsId,
            @ApplicationId,
            @ChildFirstName,
            @ChildMiddleNames,
            @ChildLastName,
            @ChildDateOfBirth,
            @ChildSex,
            @PlaceOfBirth,
            @ProvinceOfBirth,
            @CountryOfBirth,
            @MotherFirstName,
            @MotherLastName,
            @MotherIdNumber,
            @FatherFirstName,
            @FatherLastName,
            @FatherIdNumber,
            @CurrentCertificateNumber,
            @RequestReason,
            @ChangedAtUtc,
            NULL
        );
    END;


    SELECT
        detail.BirthServiceDetailsId,
        detail.ApplicationId,

        application.ApplicationType,

        detail.ChildFirstName,
        detail.ChildMiddleNames,
        detail.ChildLastName,
        detail.ChildDateOfBirth,
        detail.ChildSex,
        detail.PlaceOfBirth,
        detail.ProvinceOfBirth,
        detail.CountryOfBirth,

        detail.MotherFirstName,
        detail.MotherLastName,
        detail.MotherIdNumber,

        detail.FatherFirstName,
        detail.FatherLastName,
        detail.FatherIdNumber,

        detail.CurrentCertificateNumber,
        detail.RequestReason,

        detail.CreatedAtUtc,
        detail.UpdatedAtUtc,
        detail.RowVersion

    FROM [birth].BirthServiceDetails AS detail

    INNER JOIN [applications].Applications AS application
        ON application.ApplicationId =
            detail.ApplicationId

    WHERE detail.ApplicationId =
        @ApplicationId;
END;
GO


CREATE OR ALTER PROCEDURE
    [birth].BirthServiceDetails_GetForApplicant

    @ApplicationId UNIQUEIDENTIFIER,

    @UserId UNIQUEIDENTIFIER

AS
BEGIN
    SET NOCOUNT ON;


    SELECT
        detail.BirthServiceDetailsId,
        detail.ApplicationId,

        application.ApplicationType,

        detail.ChildFirstName,
        detail.ChildMiddleNames,
        detail.ChildLastName,
        detail.ChildDateOfBirth,
        detail.ChildSex,
        detail.PlaceOfBirth,
        detail.ProvinceOfBirth,
        detail.CountryOfBirth,

        detail.MotherFirstName,
        detail.MotherLastName,
        detail.MotherIdNumber,

        detail.FatherFirstName,
        detail.FatherLastName,
        detail.FatherIdNumber,

        detail.CurrentCertificateNumber,
        detail.RequestReason,

        detail.CreatedAtUtc,
        detail.UpdatedAtUtc,
        detail.RowVersion

    FROM [birth].BirthServiceDetails AS detail

    INNER JOIN [applications].Applications AS application
        ON application.ApplicationId =
            detail.ApplicationId

    INNER JOIN [citizens].Citizens AS applicant
        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE detail.ApplicationId =
            @ApplicationId

      AND applicant.UserId =
            @UserId

      AND applicant.IsActive = 1;
END;
GO
