USE [HomeAffairsDb];
GO

CREATE OR ALTER PROCEDURE [identity].User_RecordFailedLogin
    @UserId UNIQUEIDENTIFIER,
    @MaximumFailedLoginAttempts INT,
    @LockoutDurationMinutes INT,
    @OccurredAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @MaximumFailedLoginAttempts <= 0
    BEGIN;
        THROW 51010,
            'Maximum failed login attempts must be greater than zero.',
            1;
    END;

    IF @LockoutDurationMinutes <= 0
    BEGIN;
        THROW 51011,
            'Lockout duration must be greater than zero.',
            1;
    END;

    UPDATE [identity].Users
    SET
        FailedLoginAttempts =
            CASE
                WHEN FailedLoginAttempts + 1
                    >= @MaximumFailedLoginAttempts
                THEN 0
                ELSE FailedLoginAttempts + 1
            END,

        LockedUntilUtc =
            CASE
                WHEN FailedLoginAttempts + 1
                    >= @MaximumFailedLoginAttempts
                THEN DATEADD(
                    MINUTE,
                    @LockoutDurationMinutes,
                    @OccurredAtUtc
                )
                ELSE LockedUntilUtc
            END,

        UpdatedAtUtc = @OccurredAtUtc
    WHERE UserId = @UserId;

    IF @@ROWCOUNT = 0
    BEGIN;
        THROW 51012, 'The user account could not be found.', 1;
    END;

    SELECT
        UserId,
        FailedLoginAttempts,
        LockedUntilUtc
    FROM [identity].Users
    WHERE UserId = @UserId;
END;
GO

CREATE OR ALTER PROCEDURE [identity].User_RecordSuccessfulLogin
    @UserId UNIQUEIDENTIFIER,
    @OccurredAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [identity].Users
    SET
        FailedLoginAttempts = 0,
        LockedUntilUtc = NULL,
        UpdatedAtUtc = @OccurredAtUtc
    WHERE UserId = @UserId;

    IF @@ROWCOUNT = 0
    BEGIN;
        THROW 51013, 'The user account could not be found.', 1;
    END;
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

    IF LEN(LTRIM(RTRIM(@EventType))) = 0
    BEGIN;
        THROW 51014, 'Security event type is required.', 1;
    END;

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
