USE [HomeAffairsDb];
GO

/*
    Creates a newly issued refresh token.

    Only a SHA-512 token hash is persisted.
    The raw refresh token must never be stored.
*/
CREATE OR ALTER PROCEDURE [identity].RefreshToken_Create
    @RefreshTokenId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @TokenHash VARBINARY(64),
    @CreatedAtUtc DATETIME2(7),
    @ExpiresAtUtc DATETIME2(7),
    @CreatedByIpAddress NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ExpiresAtUtc <= @CreatedAtUtc
    BEGIN;
        THROW 51020,
            'Refresh token expiry must be later than its creation time.',
            1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [identity].Users
        WHERE UserId = @UserId
          AND IsActive = 1
    )
    BEGIN;
        THROW 51021,
            'The active user account could not be found.',
            1;
    END;

    INSERT INTO [identity].RefreshTokens
    (
        RefreshTokenId,
        UserId,
        TokenHash,
        CreatedAtUtc,
        ExpiresAtUtc,
        RevokedAtUtc,
        ReplacedByTokenId,
        CreatedByIpAddress,
        RevokedByIpAddress
    )
    VALUES
    (
        @RefreshTokenId,
        @UserId,
        @TokenHash,
        @CreatedAtUtc,
        @ExpiresAtUtc,
        NULL,
        NULL,
        @CreatedByIpAddress,
        NULL
    );
END;
GO


/*
    Rotates a valid refresh token atomically.

    Result statuses:

        ROTATED
        NOT_FOUND
        EXPIRED
        REVOKED
        USER_INACTIVE

    When REVOKED is returned, token reuse may have occurred.
    All active refresh tokens belonging to that user are revoked.
*/
CREATE OR ALTER PROCEDURE [identity].RefreshToken_Rotate
    @CurrentTokenHash VARBINARY(64),
    @NewRefreshTokenId UNIQUEIDENTIFIER,
    @NewTokenHash VARBINARY(64),
    @CreatedAtUtc DATETIME2(7),
    @ExpiresAtUtc DATETIME2(7),
    @IpAddress NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentRefreshTokenId UNIQUEIDENTIFIER;
    DECLARE @UserId UNIQUEIDENTIFIER;
    DECLARE @CurrentExpiresAtUtc DATETIME2(7);
    DECLARE @CurrentRevokedAtUtc DATETIME2(7);
    DECLARE @UserIsActive BIT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @CurrentRefreshTokenId = rt.RefreshTokenId,
            @UserId = rt.UserId,
            @CurrentExpiresAtUtc = rt.ExpiresAtUtc,
            @CurrentRevokedAtUtc = rt.RevokedAtUtc,
            @UserIsActive = u.IsActive
        FROM [identity].RefreshTokens AS rt
        INNER JOIN [identity].Users AS u
            ON u.UserId = rt.UserId
        WHERE rt.TokenHash = @CurrentTokenHash;

        IF @CurrentRefreshTokenId IS NULL
        BEGIN
            COMMIT TRANSACTION;

            SELECT
                CAST(N'NOT_FOUND' AS NVARCHAR(30)) AS RotationStatus,
                CAST(NULL AS UNIQUEIDENTIFIER) AS UserId;

            RETURN;
        END;

        /*
            Lock the current token row before evaluating or changing it.

            This prevents two concurrent refresh requests from both
            successfully rotating the same token.
        */
        SELECT
            @CurrentRefreshTokenId = rt.RefreshTokenId,
            @UserId = rt.UserId,
            @CurrentExpiresAtUtc = rt.ExpiresAtUtc,
            @CurrentRevokedAtUtc = rt.RevokedAtUtc
        FROM [identity].RefreshTokens AS rt
            WITH (UPDLOCK, HOLDLOCK)
        WHERE rt.RefreshTokenId = @CurrentRefreshTokenId;

        IF @UserIsActive = 0
        BEGIN
            UPDATE [identity].RefreshTokens
            SET
                RevokedAtUtc =
                    COALESCE(RevokedAtUtc, @CreatedAtUtc),
                RevokedByIpAddress =
                    COALESCE(RevokedByIpAddress, @IpAddress)
            WHERE UserId = @UserId
              AND RevokedAtUtc IS NULL;

            COMMIT TRANSACTION;

            SELECT
                CAST(N'USER_INACTIVE' AS NVARCHAR(30))
                    AS RotationStatus,
                @UserId AS UserId;

            RETURN;
        END;

        IF @CurrentRevokedAtUtc IS NOT NULL
        BEGIN
            /*
                A revoked token has been submitted again.

                Revoke the complete active token family for the user.
            */
            UPDATE [identity].RefreshTokens
            SET
                RevokedAtUtc =
                    COALESCE(RevokedAtUtc, @CreatedAtUtc),
                RevokedByIpAddress =
                    COALESCE(RevokedByIpAddress, @IpAddress)
            WHERE UserId = @UserId
              AND RevokedAtUtc IS NULL;

            COMMIT TRANSACTION;

            SELECT
                CAST(N'REVOKED' AS NVARCHAR(30))
                    AS RotationStatus,
                @UserId AS UserId;

            RETURN;
        END;

        IF @CurrentExpiresAtUtc <= @CreatedAtUtc
        BEGIN
            UPDATE [identity].RefreshTokens
            SET
                RevokedAtUtc = @CreatedAtUtc,
                RevokedByIpAddress = @IpAddress
            WHERE RefreshTokenId = @CurrentRefreshTokenId;

            COMMIT TRANSACTION;

            SELECT
                CAST(N'EXPIRED' AS NVARCHAR(30))
                    AS RotationStatus,
                @UserId AS UserId;

            RETURN;
        END;

        INSERT INTO [identity].RefreshTokens
        (
            RefreshTokenId,
            UserId,
            TokenHash,
            CreatedAtUtc,
            ExpiresAtUtc,
            RevokedAtUtc,
            ReplacedByTokenId,
            CreatedByIpAddress,
            RevokedByIpAddress
        )
        VALUES
        (
            @NewRefreshTokenId,
            @UserId,
            @NewTokenHash,
            @CreatedAtUtc,
            @ExpiresAtUtc,
            NULL,
            NULL,
            @IpAddress,
            NULL
        );

        UPDATE [identity].RefreshTokens
        SET
            RevokedAtUtc = @CreatedAtUtc,
            ReplacedByTokenId = @NewRefreshTokenId,
            RevokedByIpAddress = @IpAddress
        WHERE RefreshTokenId = @CurrentRefreshTokenId;

        COMMIT TRANSACTION;

        SELECT
            CAST(N'ROTATED' AS NVARCHAR(30))
                AS RotationStatus,
            @UserId AS UserId;
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
    Revokes a refresh token during logout.

    The operation is idempotent. Logging out more than once does not
    produce a database error.
*/
CREATE OR ALTER PROCEDURE [identity].RefreshToken_Revoke
    @TokenHash VARBINARY(64),
    @RevokedAtUtc DATETIME2(7),
    @RevokedByIpAddress NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserId UNIQUEIDENTIFIER;

    SELECT
        @UserId = rt.UserId
    FROM [identity].RefreshTokens AS rt
    WHERE rt.TokenHash = @TokenHash;

    IF @UserId IS NULL
    BEGIN
        SELECT
            CAST(0 AS BIT) AS TokenFound,
            CAST(NULL AS UNIQUEIDENTIFIER) AS UserId;

        RETURN;
    END;

    UPDATE [identity].RefreshTokens
    SET
        RevokedAtUtc =
            COALESCE(RevokedAtUtc, @RevokedAtUtc),
        RevokedByIpAddress =
            COALESCE(
                RevokedByIpAddress,
                @RevokedByIpAddress
            )
    WHERE TokenHash = @TokenHash;

    SELECT
        CAST(1 AS BIT) AS TokenFound,
        @UserId AS UserId;
END;
GO


/*
    Revokes every active refresh token for a user.

    This will later support:
    - logout from all devices
    - password reset
    - suspected account compromise
    - administrative account suspension
*/
CREATE OR ALTER PROCEDURE [identity].RefreshToken_RevokeAllForUser
    @UserId UNIQUEIDENTIFIER,
    @RevokedAtUtc DATETIME2(7),
    @RevokedByIpAddress NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [identity].RefreshTokens
    SET
        RevokedAtUtc = @RevokedAtUtc,
        RevokedByIpAddress = @RevokedByIpAddress
    WHERE UserId = @UserId
      AND RevokedAtUtc IS NULL;

    SELECT
        @@ROWCOUNT AS RevokedTokenCount;
END;
GO
