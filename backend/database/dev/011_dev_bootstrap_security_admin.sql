USE [HomeAffairsDb];
GO

DECLARE @UserId UNIQUEIDENTIFIER;
DECLARE @RoleId UNIQUEIDENTIFIER;

SELECT
    @UserId = UserId
FROM [identity].Users
WHERE NormalizedEmail =
    N'SABELO.TEST@EXAMPLE.COM';

SELECT
    @RoleId = RoleId
FROM [identity].Roles
WHERE Code =
    N'SECURITY_ADMINISTRATOR';

IF @UserId IS NULL
BEGIN;
    THROW 51050,
        'Development bootstrap user was not found.',
        1;
END;

IF @RoleId IS NULL
BEGIN;
    THROW 51051,
        'SECURITY_ADMINISTRATOR role was not found.',
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
        SYSUTCDATETIME(),
        @UserId
    );
END;
GO
