USE [HomeAffairsDb];
GO

IF OBJECT_ID(
    N'[citizens].Addresses',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [citizens].Addresses
    (
        AddressId UNIQUEIDENTIFIER NOT NULL,
        CitizenId UNIQUEIDENTIFIER NOT NULL,

        AddressType NVARCHAR(30) NOT NULL,

        AddressLine1 NVARCHAR(200) NOT NULL,
        AddressLine2 NVARCHAR(200) NULL,

        Suburb NVARCHAR(150) NULL,
        CityOrTown NVARCHAR(150) NOT NULL,
        Province NVARCHAR(100) NULL,
        PostalCode NVARCHAR(20) NULL,

        CountryCode CHAR(2) NOT NULL
            CONSTRAINT DF_Addresses_CountryCode
            DEFAULT 'ZA',

        IsPrimary BIT NOT NULL
            CONSTRAINT DF_Addresses_IsPrimary
            DEFAULT 0,

        IsActive BIT NOT NULL
            CONSTRAINT DF_Addresses_IsActive
            DEFAULT 1,

        CreatedAtUtc DATETIME2(7) NOT NULL,
        UpdatedAtUtc DATETIME2(7) NULL,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_Addresses
            PRIMARY KEY CLUSTERED (AddressId),

        CONSTRAINT FK_Addresses_Citizen
            FOREIGN KEY (CitizenId)
            REFERENCES [citizens].Citizens (CitizenId),

        CONSTRAINT CK_Addresses_Type
            CHECK
            (
                AddressType IN
                (
                    N'RESIDENTIAL',
                    N'POSTAL'
                )
            ),

        CONSTRAINT CK_Addresses_Line1_NotEmpty
            CHECK
            (
                LEN(LTRIM(RTRIM(AddressLine1))) > 0
            ),

        CONSTRAINT CK_Addresses_City_NotEmpty
            CHECK
            (
                LEN(LTRIM(RTRIM(CityOrTown))) > 0
            ),

        CONSTRAINT CK_Addresses_Country_NotEmpty
            CHECK
            (
                LEN(LTRIM(RTRIM(CountryCode))) = 2
            )
    );

    CREATE UNIQUE NONCLUSTERED INDEX
        UX_Addresses_Citizen_PrimaryType
    ON [citizens].Addresses
    (
        CitizenId,
        AddressType
    )
    WHERE IsPrimary = 1
      AND IsActive = 1;

    CREATE NONCLUSTERED INDEX
        IX_Addresses_CitizenId
    ON [citizens].Addresses
    (
        CitizenId,
        IsActive
    );
END;
GO


CREATE OR ALTER PROCEDURE
    [citizens].Address_GetByCitizenId
    @CitizenId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.AddressId,
        a.CitizenId,
        a.AddressType,
        a.AddressLine1,
        a.AddressLine2,
        a.Suburb,
        a.CityOrTown,
        a.Province,
        a.PostalCode,
        a.CountryCode,
        a.IsPrimary,
        a.IsActive,
        a.CreatedAtUtc,
        a.UpdatedAtUtc,
        a.RowVersion
    FROM [citizens].Addresses AS a
    WHERE a.CitizenId = @CitizenId
      AND a.IsActive = 1
    ORDER BY
        a.AddressType,
        a.IsPrimary DESC,
        a.CreatedAtUtc;
END;
GO


CREATE OR ALTER PROCEDURE
    [citizens].Address_GetByIdForUser
    @AddressId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.AddressId,
        a.CitizenId,
        a.AddressType,
        a.AddressLine1,
        a.AddressLine2,
        a.Suburb,
        a.CityOrTown,
        a.Province,
        a.PostalCode,
        a.CountryCode,
        a.IsPrimary,
        a.IsActive,
        a.CreatedAtUtc,
        a.UpdatedAtUtc,
        a.RowVersion
    FROM [citizens].Addresses AS a
    INNER JOIN [citizens].Citizens AS c
        ON c.CitizenId = a.CitizenId
    WHERE a.AddressId = @AddressId
      AND c.UserId = @UserId
      AND c.IsActive = 1
      AND a.IsActive = 1;
END;
GO


CREATE OR ALTER PROCEDURE
    [citizens].Address_Create
    @AddressId UNIQUEIDENTIFIER,
    @CitizenId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @AddressType NVARCHAR(30),
    @AddressLine1 NVARCHAR(200),
    @AddressLine2 NVARCHAR(200) = NULL,
    @Suburb NVARCHAR(150) = NULL,
    @CityOrTown NVARCHAR(150),
    @Province NVARCHAR(100) = NULL,
    @PostalCode NVARCHAR(20) = NULL,
    @CountryCode CHAR(2),
    @IsPrimary BIT,
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
            WHERE CitizenId = @CitizenId
              AND UserId = @UserId
              AND IsActive = 1
        )
        BEGIN;
            THROW 52201,
                'The citizen profile could not be found.',
                1;
        END;

        IF @AddressType NOT IN
        (
            N'RESIDENTIAL',
            N'POSTAL'
        )
        BEGIN;
            THROW 52202,
                'The address type is not valid.',
                1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [citizens].Addresses
            WHERE CitizenId = @CitizenId
              AND AddressType = @AddressType
              AND IsActive = 1
        )
        BEGIN
            SET @IsPrimary = 1;
        END;

        IF @IsPrimary = 1
        BEGIN
            UPDATE [citizens].Addresses
            SET
                IsPrimary = 0,
                UpdatedAtUtc = @CreatedAtUtc
            WHERE CitizenId = @CitizenId
              AND AddressType = @AddressType
              AND IsPrimary = 1
              AND IsActive = 1;
        END;

        INSERT INTO [citizens].Addresses
        (
            AddressId,
            CitizenId,
            AddressType,
            AddressLine1,
            AddressLine2,
            Suburb,
            CityOrTown,
            Province,
            PostalCode,
            CountryCode,
            IsPrimary,
            IsActive,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            @AddressId,
            @CitizenId,
            @AddressType,
            @AddressLine1,
            @AddressLine2,
            @Suburb,
            @CityOrTown,
            @Province,
            @PostalCode,
            @CountryCode,
            @IsPrimary,
            1,
            @CreatedAtUtc,
            NULL
        );

        COMMIT TRANSACTION;

        EXEC [citizens].Address_GetByIdForUser
            @AddressId = @AddressId,
            @UserId = @UserId;
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
    [citizens].Address_Update
    @AddressId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @AddressType NVARCHAR(30),
    @AddressLine1 NVARCHAR(200),
    @AddressLine2 NVARCHAR(200) = NULL,
    @Suburb NVARCHAR(150) = NULL,
    @CityOrTown NVARCHAR(150),
    @Province NVARCHAR(100) = NULL,
    @PostalCode NVARCHAR(20) = NULL,
    @CountryCode CHAR(2),
    @IsPrimary BIT,
    @ExpectedRowVersion BINARY(8),
    @UpdatedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CitizenId UNIQUEIDENTIFIER;
        DECLARE @OldAddressType NVARCHAR(30);

        SELECT
            @CitizenId = a.CitizenId,
            @OldAddressType = a.AddressType
        FROM [citizens].Addresses AS a
        INNER JOIN [citizens].Citizens AS c
            ON c.CitizenId = a.CitizenId
        WHERE a.AddressId = @AddressId
          AND c.UserId = @UserId
          AND a.IsActive = 1
          AND c.IsActive = 1;

        IF @CitizenId IS NULL
        BEGIN;
            THROW 52203,
                'The address could not be found.',
                1;
        END;

        IF @AddressType NOT IN
        (
            N'RESIDENTIAL',
            N'POSTAL'
        )
        BEGIN;
            THROW 52202,
                'The address type is not valid.',
                1;
        END;

        IF @IsPrimary = 1
        BEGIN
            UPDATE [citizens].Addresses
            SET
                IsPrimary = 0,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE CitizenId = @CitizenId
              AND AddressType = @AddressType
              AND AddressId <> @AddressId
              AND IsPrimary = 1
              AND IsActive = 1;
        END;

        UPDATE [citizens].Addresses
        SET
            AddressType = @AddressType,
            AddressLine1 = @AddressLine1,
            AddressLine2 = @AddressLine2,
            Suburb = @Suburb,
            CityOrTown = @CityOrTown,
            Province = @Province,
            PostalCode = @PostalCode,
            CountryCode = @CountryCode,
            IsPrimary = @IsPrimary,
            UpdatedAtUtc = @UpdatedAtUtc
        WHERE AddressId = @AddressId
          AND CitizenId = @CitizenId
          AND IsActive = 1
          AND RowVersion = @ExpectedRowVersion;

        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 52204,
                'The address was modified by another request.',
                1;
        END;

        IF @OldAddressType <> @AddressType
           AND NOT EXISTS
        (
            SELECT 1
            FROM [citizens].Addresses
            WHERE CitizenId = @CitizenId
              AND AddressType = @OldAddressType
              AND IsPrimary = 1
              AND IsActive = 1
        )
        BEGIN
            DECLARE @OldReplacement UNIQUEIDENTIFIER;

            SELECT TOP (1)
                @OldReplacement = AddressId
            FROM [citizens].Addresses
            WHERE CitizenId = @CitizenId
              AND AddressType = @OldAddressType
              AND IsActive = 1
            ORDER BY
                CreatedAtUtc,
                AddressId;

            IF @OldReplacement IS NOT NULL
            BEGIN
                UPDATE [citizens].Addresses
                SET
                    IsPrimary = 1,
                    UpdatedAtUtc = @UpdatedAtUtc
                WHERE AddressId = @OldReplacement;
            END;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [citizens].Addresses
            WHERE CitizenId = @CitizenId
              AND AddressType = @AddressType
              AND IsPrimary = 1
              AND IsActive = 1
        )
        BEGIN
            UPDATE [citizens].Addresses
            SET
                IsPrimary = 1,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE AddressId = @AddressId;
        END;

        COMMIT TRANSACTION;

        EXEC [citizens].Address_GetByIdForUser
            @AddressId = @AddressId,
            @UserId = @UserId;
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
    [citizens].Address_Delete
    @AddressId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @ExpectedRowVersion BINARY(8),
    @DeletedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CitizenId UNIQUEIDENTIFIER;
        DECLARE @AddressType NVARCHAR(30);
        DECLARE @WasPrimary BIT;

        SELECT
            @CitizenId = a.CitizenId,
            @AddressType = a.AddressType,
            @WasPrimary = a.IsPrimary
        FROM [citizens].Addresses AS a
        INNER JOIN [citizens].Citizens AS c
            ON c.CitizenId = a.CitizenId
        WHERE a.AddressId = @AddressId
          AND c.UserId = @UserId
          AND a.IsActive = 1
          AND c.IsActive = 1;

        IF @CitizenId IS NULL
        BEGIN;
            THROW 52203,
                'The address could not be found.',
                1;
        END;

        UPDATE [citizens].Addresses
        SET
            IsPrimary = 0,
            IsActive = 0,
            UpdatedAtUtc = @DeletedAtUtc
        WHERE AddressId = @AddressId
          AND RowVersion = @ExpectedRowVersion
          AND IsActive = 1;

        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 52204,
                'The address was modified by another request.',
                1;
        END;

        IF @WasPrimary = 1
        BEGIN
            DECLARE @Replacement UNIQUEIDENTIFIER;

            SELECT TOP (1)
                @Replacement = AddressId
            FROM [citizens].Addresses
            WHERE CitizenId = @CitizenId
              AND AddressType = @AddressType
              AND IsActive = 1
            ORDER BY
                CreatedAtUtc,
                AddressId;

            IF @Replacement IS NOT NULL
            BEGIN
                UPDATE [citizens].Addresses
                SET
                    IsPrimary = 1,
                    UpdatedAtUtc = @DeletedAtUtc
                WHERE AddressId = @Replacement;
            END;
        END;

        COMMIT TRANSACTION;
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
