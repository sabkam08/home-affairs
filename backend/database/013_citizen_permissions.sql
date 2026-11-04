USE [HomeAffairsDb];
GO

DECLARE @NowUtc DATETIME2(7) =
    SYSUTCDATETIME();

IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code =
        N'PROFILE.MANAGE_SELF'
)
BEGIN
    INSERT INTO [identity].Permissions
    (
        PermissionId,
        Code,
        Name,
        Description,
        CreatedAtUtc
    )
    VALUES
    (
        NEWID(),
        N'PROFILE.MANAGE_SELF',
        N'Manage Own Citizen Profile',
        N'Allows a citizen to create and update their own citizen profile.',
        @NowUtc
    );
END;
GO


INSERT INTO [identity].RolePermissions
(
    RoleId,
    PermissionId,
    AssignedAtUtc
)
SELECT
    r.RoleId,
    p.PermissionId,
    SYSUTCDATETIME()
FROM [identity].Roles AS r
CROSS JOIN [identity].Permissions AS p
WHERE r.Code = N'CITIZEN'
  AND p.Code =
      N'PROFILE.MANAGE_SELF'
  AND NOT EXISTS
  (
      SELECT 1
      FROM [identity].RolePermissions AS rp
      WHERE rp.RoleId = r.RoleId
        AND rp.PermissionId =
            p.PermissionId
  );
GO
