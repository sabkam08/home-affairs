USE [HomeAffairsDb];
GO

/*
    ============================================================
    Citizen_GetById
    ============================================================
*/

CREATE OR ALTER PROCEDURE [citizens].Citizen_GetById
    @CitizenId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.CitizenId,
        c.UserId,
        c.SouthAfricanIdNumber,
        c.FirstName,
        c.MiddleNames,
        c.LastName,
        c.DateOfBirth,
        c.Sex,
        c.IsActive,
        c.CreatedAtUtc,
        c.UpdatedAtUtc,
        c.RowVersion
    FROM [citizens].Citizens AS c
    WHERE c.CitizenId = @CitizenId;
END;
GO


/*
    ============================================================
    Citizen_GetByUserId
    ============================================================
*/

CREATE OR ALTER PROCEDURE [citizens].Citizen_GetByUserId
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.CitizenId,
        c.UserId,
        c.SouthAfricanIdNumber,
        c.FirstName,
        c.MiddleNames,
        c.LastName,
        c.DateOfBirth,
        c.Sex,
        c.IsActive,
        c.CreatedAtUtc,
        c.UpdatedAtUtc,
        c.RowVersion
    FROM [citizens].Citizens AS c
    WHERE c.UserId = @UserId;
END;
GO


/*
    ============================================================
    Citizen_CreateForUser
    ============================================================
*/

CREATE OR ALTER PROCEDURE [citizens].Citizen_CreateForUser
    @CitizenId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @SouthAfricanIdNumber NVARCHAR(13) = NULL,
    @FirstName NVARCHAR(100),
    @MiddleNames NVARCHAR(200) = NULL,
    @LastName NVARCHAR(100),
    @DateOfBirth DATE,
    @Sex CHAR(1),
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
            FROM [identity].Users
            WHERE UserId = @UserId
              AND IsActive = 1
        )
        BEGIN;
            THROW 52001,
                'The active user account could not be found.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [citizens].Citizens
                WITH (UPDLOCK, HOLDLOCK)
            WHERE UserId = @UserId
        )
        BEGIN;
            THROW 52002,
                'A citizen profile already exists for this user.',
                1;
        END;

        IF @SouthAfricanIdNumber IS NOT NULL
           AND EXISTS
        (
            SELECT 1
            FROM [citizens].Citizens
                WITH (UPDLOCK, HOLDLOCK)
            WHERE SouthAfricanIdNumber =
                @SouthAfricanIdNumber
        )
        BEGIN;
            THROW 52003,
                'A citizen with this South African ID number already exists.',
                1;
        END;

        INSERT INTO [citizens].Citizens
        (
            CitizenId,
            UserId,
            SouthAfricanIdNumber,
            FirstName,
            MiddleNames,
            LastName,
            DateOfBirth,
            Sex,
            IsActive,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            @CitizenId,
            @UserId,
            @SouthAfricanIdNumber,
            @FirstName,
            @MiddleNames,
            @LastName,
            @DateOfBirth,
            @Sex,
            1,
            @CreatedAtUtc,
            NULL
        );

        COMMIT TRANSACTION;

        SELECT
            c.CitizenId,
            c.UserId,
            c.SouthAfricanIdNumber,
            c.FirstName,
            c.MiddleNames,
            c.LastName,
            c.DateOfBirth,
            c.Sex,
            c.IsActive,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            c.RowVersion
        FROM [citizens].Citizens AS c
        WHERE c.CitizenId = @CitizenId;
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
    Citizen_UpdateOwnProfile

    RowVersion provides optimistic-concurrency protection.
    ============================================================
*/

CREATE OR ALTER PROCEDURE [citizens].Citizen_UpdateOwnProfile
    @CitizenId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @SouthAfricanIdNumber NVARCHAR(13) = NULL,
    @FirstName NVARCHAR(100),
    @MiddleNames NVARCHAR(200) = NULL,
    @LastName NVARCHAR(100),
    @DateOfBirth DATE,
    @Sex CHAR(1),
    @ExpectedRowVersion BINARY(8),
    @UpdatedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @SouthAfricanIdNumber IS NOT NULL
       AND EXISTS
    (
        SELECT 1
        FROM [citizens].Citizens
        WHERE SouthAfricanIdNumber =
            @SouthAfricanIdNumber
          AND CitizenId <> @CitizenId
    )
    BEGIN;
        THROW 52003,
            'A citizen with this South African ID number already exists.',
            1;
    END;

    UPDATE [citizens].Citizens
    SET
        SouthAfricanIdNumber =
            @SouthAfricanIdNumber,

        FirstName =
            @FirstName,

        MiddleNames =
            @MiddleNames,

        LastName =
            @LastName,

        DateOfBirth =
            @DateOfBirth,

        Sex =
            @Sex,

        UpdatedAtUtc =
            @UpdatedAtUtc

    WHERE CitizenId = @CitizenId
      AND UserId = @UserId
      AND RowVersion = @ExpectedRowVersion
      AND IsActive = 1;

    IF @@ROWCOUNT = 0
    BEGIN
        IF NOT EXISTS
        (
            SELECT 1
            FROM [citizens].Citizens
            WHERE CitizenId = @CitizenId
              AND UserId = @UserId
        )
        BEGIN;
            THROW 52004,
                'The citizen profile could not be found.',
                1;
        END;

        THROW 52005,
            'The citizen profile was modified by another request.',
            1;
    END;

    SELECT
        c.CitizenId,
        c.UserId,
        c.SouthAfricanIdNumber,
        c.FirstName,
        c.MiddleNames,
        c.LastName,
        c.DateOfBirth,
        c.Sex,
        c.IsActive,
        c.CreatedAtUtc,
        c.UpdatedAtUtc,
        c.RowVersion
    FROM [citizens].Citizens AS c
    WHERE c.CitizenId = @CitizenId;
END;
GO
