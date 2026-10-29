USE [HomeAffairsDb];
GO

CREATE OR ALTER PROCEDURE [identity].User_GetByEmail
    @NormalizedEmail NVARCHAR(320)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.Email,
        u.NormalizedEmail,
        u.PasswordHash,
        u.FirstName,
        u.LastName,
        u.IsActive,
        u.EmailVerifiedAtUtc,
        u.FailedLoginAttempts,
        u.LockedUntilUtc,
        u.CreatedAtUtc,
        u.UpdatedAtUtc
    FROM [identity].Users AS u
    WHERE u.NormalizedEmail = @NormalizedEmail;
END;
GO

CREATE OR ALTER PROCEDURE [identity].User_GetById
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.Email,
        u.NormalizedEmail,
        u.PasswordHash,
        u.FirstName,
        u.LastName,
        u.IsActive,
        u.EmailVerifiedAtUtc,
        u.FailedLoginAttempts,
        u.LockedUntilUtc,
        u.CreatedAtUtc,
        u.UpdatedAtUtc
    FROM [identity].Users AS u
    WHERE u.UserId = @UserId;
END;
GO

CREATE OR ALTER PROCEDURE [identity].User_CreateCitizen
    @UserId UNIQUEIDENTIFIER,
    @Email NVARCHAR(320),
    @NormalizedEmail NVARCHAR(320),
    @PasswordHash NVARCHAR(1000),
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @CreatedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- The TRY...CATCH block must safely encapsulate your transaction logic 
    -- directly within the procedure execution sequence.
    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS
        (
            SELECT 1
            FROM [identity].Users WITH (UPDLOCK, HOLDLOCK)
            WHERE NormalizedEmail = @NormalizedEmail
        )
        BEGIN
            -- Terminating statement with a semicolon is mandatory before a THROW
            RAISERROR('A user with this email address already exists.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        DECLARE @CitizenRoleId UNIQUEIDENTIFIER;

        SELECT
            @CitizenRoleId = RoleId
        FROM [identity].Roles
        WHERE Code = N'CITIZEN';

        IF @CitizenRoleId IS NULL
        BEGIN
            RAISERROR('The CITIZEN role has not been configured.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
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
            CreatedAtUtc
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
            @CreatedAtUtc
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
            @CitizenRoleId,
            @CreatedAtUtc,
            NULL
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        -- Re-throw the original error captured by the engine
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [identity].User_GetRoles
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.Code
    FROM [identity].UserRoles AS ur
    INNER JOIN [identity].Roles AS r
        ON r.RoleId = ur.RoleId
    WHERE ur.UserId = @UserId
    ORDER BY r.Code;
END;
GO

CREATE OR ALTER PROCEDURE [identity].SecurityEvent_Create
    @SecurityEventId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER = NULL,
    @EventType NVARCHAR(100),
    @Description NVARCHAR(1000) = NULL,
    @IpAddress NVARCHAR(64) = NULL,
    @UserAgent NVARCHAR(1000) = NULL,
    @CorrelationId NVARCHAR(100) = NULL,
    @OccurredAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [identity].SecurityEvents
    (
        SecurityEventId,
        UserId,
        EventType,
        Description,
        IpAddress,
        UserAgent,
        CorrelationId,
        OccurredAtUtc
    )
    VALUES
    (
        @SecurityEventId,
        @UserId,
        @EventType,
        @Description,
        @IpAddress,
        @UserAgent,
        @CorrelationId,
        @OccurredAtUtc
    );
END;
GO
