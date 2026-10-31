USE [HomeAffairsDb];
GO

/*
    Creates an internal staff account.

    The role is resolved inside SQL from a controlled role code.
*/
CREATE OR ALTER PROCEDURE [identity].User_CreateStaff
    @UserId UNIQUEIDENTIFIER,
    @Email NVARCHAR(320),
    @NormalizedEmail NVARCHAR(320),
    @PasswordHash NVARCHAR(1000),
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @RoleCode NVARCHAR(100),
    @CreatedAtUtc DATETIME2(7),
    @CreatedByUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS
        (
            SELECT 1
            FROM [identity].Users WITH (UPDLOCK, HOLDLOCK)
            WHERE NormalizedEmail = @NormalizedEmail
        )
        BEGIN;
            THROW 51030,
                'A user with this email address already exists.',
                1;
        END;

        IF @RoleCode NOT IN
        (
            N'PRACTITIONER',
            N'SUPERVISOR',
            N'ADMINISTRATOR',
            N'SECURITY_ADMINISTRATOR'
        )
        BEGIN;
            THROW 51031,
                'The supplied staff role is not valid.',
                1;
        END;

        DECLARE @RoleId UNIQUEIDENTIFIER;

        SELECT
            @RoleId = RoleId
        FROM [identity].Roles
        WHERE Code = @RoleCode;

        IF @RoleId IS NULL
        BEGIN;
            THROW 51032,
                'The requested staff role has not been configured.',
                1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [identity].Users
            WHERE UserId = @CreatedByUserId
              AND IsActive = 1
        )
        BEGIN;
            THROW 51033,
                'The creating user could not be found.',
                1;
        END;

        INSERT INTO [identity].Users
        (
            UserId,
            Email,
            NormalizedEmail,
            PasswordHash,
            FirstName,
            LastName,
            IsActive,
            EmailVerifiedAtUtc,
            FailedLoginAttempts,
            LockedUntilUtc,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            @UserId,
            @Email,
            @NormalizedEmail,
            @PasswordHash,
            @FirstName,
            @LastName,
            1,
            NULL,
            0,
            NULL,
            @CreatedAtUtc,
            NULL
        );

        INSERT INTO [identity].UserRoles
        (
            UserId,
            RoleId,
            AssignedAtUtc,
            AssignedByUserId
        )
        VALUES
        (
            @UserId,
            @RoleId,
            @CreatedAtUtc,
            @CreatedByUserId
        );

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


CREATE OR ALTER PROCEDURE [identity].User_AssignRole
    @UserId UNIQUEIDENTIFIER,
    @RoleCode NVARCHAR(100),
    @AssignedAtUtc DATETIME2(7),
    @AssignedByUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @RoleCode NOT IN
    (
        N'CITIZEN',
        N'PRACTITIONER',
        N'SUPERVISOR',
        N'ADMINISTRATOR',
        N'SECURITY_ADMINISTRATOR'
    )
    BEGIN;
        THROW 51034,
            'The supplied role is not valid.',
            1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [identity].Users
        WHERE UserId = @UserId
    )
    BEGIN;
        THROW 51035,
            'The target user could not be found.',
            1;
    END;

    DECLARE @RoleId UNIQUEIDENTIFIER;

    SELECT
        @RoleId = RoleId
    FROM [identity].Roles
    WHERE Code = @RoleCode;

    IF @RoleId IS NULL
    BEGIN;
        THROW 51036,
            'The requested role has not been configured.',
            1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [identity].UserRoles
        WHERE UserId = @UserId
          AND RoleId = @RoleId
    )
    BEGIN
        INSERT INTO [identity].UserRoles
        (
            UserId,
            RoleId,
            AssignedAtUtc,
            AssignedByUserId
        )
        VALUES
        (
            @UserId,
            @RoleId,
            @AssignedAtUtc,
            @AssignedByUserId
        );
    END;
END;
GO


CREATE OR ALTER PROCEDURE [identity].User_RemoveRole
    @UserId UNIQUEIDENTIFIER,
    @RoleCode NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RoleId UNIQUEIDENTIFIER;

    SELECT
        @RoleId = RoleId
    FROM [identity].Roles
    WHERE Code = @RoleCode;

    IF @RoleId IS NULL
    BEGIN;
        THROW 51037,
            'The requested role could not be found.',
            1;
    END;

    DELETE FROM [identity].UserRoles
    WHERE UserId = @UserId
      AND RoleId = @RoleId;
END;
GO
