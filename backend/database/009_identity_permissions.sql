USE [HomeAffairsDb];
GO

/*
    ============================================================
    Identity Permissions

    Permissions describe capabilities.
    Roles are collections of permissions.

    Authorization should generally depend on permissions rather
    than hard-coded role checks throughout application code.
    ============================================================
*/


IF OBJECT_ID(N'[identity].Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].Permissions
    (
        PermissionId UNIQUEIDENTIFIER NOT NULL,
        Code NVARCHAR(150) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,

        CONSTRAINT PK_Permissions
            PRIMARY KEY CLUSTERED (PermissionId),

        CONSTRAINT UQ_Permissions_Code
            UNIQUE (Code),

        CONSTRAINT CK_Permissions_Code_NotEmpty
            CHECK (
                LEN(LTRIM(RTRIM(Code))) > 0
            )
    );
END;
GO


IF OBJECT_ID(N'[identity].RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].RolePermissions
    (
        RoleId UNIQUEIDENTIFIER NOT NULL,
        PermissionId UNIQUEIDENTIFIER NOT NULL,
        AssignedAtUtc DATETIME2(7) NOT NULL,

        CONSTRAINT PK_RolePermissions
            PRIMARY KEY CLUSTERED (
                RoleId,
                PermissionId
            ),

        CONSTRAINT FK_RolePermissions_Role
            FOREIGN KEY (RoleId)
            REFERENCES [identity].Roles (RoleId),

        CONSTRAINT FK_RolePermissions_Permission
            FOREIGN KEY (PermissionId)
            REFERENCES [identity].Permissions (PermissionId)
    );
END;
GO


/*
    ============================================================
    Seed permissions
    ============================================================
*/

DECLARE @NowUtc DATETIME2(7) = SYSUTCDATETIME();


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'PROFILE.READ_SELF'
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
        N'PROFILE.READ_SELF',
        N'Read Own Profile',
        N'Allows a user to read their own profile.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'APPLICATION.CREATE'
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
        N'APPLICATION.CREATE',
        N'Create Application',
        N'Allows creation of a Home Affairs application.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'APPLICATION.READ_OWN'
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
        N'APPLICATION.READ_OWN',
        N'Read Own Applications',
        N'Allows citizens to read applications accessible to them.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'APPLICATION.READ_ALL'
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
        N'APPLICATION.READ_ALL',
        N'Read Applications',
        N'Allows authorised staff to read applications.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'APPLICATION.PROCESS'
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
        N'APPLICATION.PROCESS',
        N'Process Applications',
        N'Allows a practitioner to process applications.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'APPLICATION.REASSIGN'
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
        N'APPLICATION.REASSIGN',
        N'Reassign Applications',
        N'Allows an application assignment to be changed.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'APPLICATION.APPROVE'
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
        N'APPLICATION.APPROVE',
        N'Approve Applications',
        N'Allows supervisor-level application approval.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'DOCUMENT.READ'
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
        N'DOCUMENT.READ',
        N'Read Documents',
        N'Allows authorised access to documents.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'DOCUMENT.UPLOAD'
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
        N'DOCUMENT.UPLOAD',
        N'Upload Documents',
        N'Allows documents to be uploaded.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'IDENTITY.USER_MANAGE'
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
        N'IDENTITY.USER_MANAGE',
        N'Manage Users',
        N'Allows authorised administration of user accounts.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'IDENTITY.ROLE_MANAGE'
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
        N'IDENTITY.ROLE_MANAGE',
        N'Manage Roles',
        N'Allows authorised administration of user role assignments.',
        @NowUtc
    );
END;


IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Permissions
    WHERE Code = N'AUDIT.READ'
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
        N'AUDIT.READ',
        N'Read Audit Records',
        N'Allows authorised review of audit information.',
        @NowUtc
    );
END;
GO


/*
    ============================================================
    CITIZEN permissions
    ============================================================
*/

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
  AND p.Code IN
  (
      N'PROFILE.READ_SELF',
      N'APPLICATION.CREATE',
      N'APPLICATION.READ_OWN',
      N'DOCUMENT.READ',
      N'DOCUMENT.UPLOAD'
  )
  AND NOT EXISTS
  (
      SELECT 1
      FROM [identity].RolePermissions AS rp
      WHERE rp.RoleId = r.RoleId
        AND rp.PermissionId = p.PermissionId
  );
GO


/*
    ============================================================
    PRACTITIONER permissions
    ============================================================
*/

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
WHERE r.Code = N'PRACTITIONER'
  AND p.Code IN
  (
      N'PROFILE.READ_SELF',
      N'APPLICATION.READ_ALL',
      N'APPLICATION.PROCESS',
      N'APPLICATION.REASSIGN',
      N'DOCUMENT.READ',
      N'DOCUMENT.UPLOAD'
  )
  AND NOT EXISTS
  (
      SELECT 1
      FROM [identity].RolePermissions AS rp
      WHERE rp.RoleId = r.RoleId
        AND rp.PermissionId = p.PermissionId
  );
GO


/*
    ============================================================
    SUPERVISOR permissions
    ============================================================
*/

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
WHERE r.Code = N'SUPERVISOR'
  AND p.Code IN
  (
      N'PROFILE.READ_SELF',
      N'APPLICATION.READ_ALL',
      N'APPLICATION.PROCESS',
      N'APPLICATION.REASSIGN',
      N'APPLICATION.APPROVE',
      N'DOCUMENT.READ',
      N'DOCUMENT.UPLOAD',
      N'AUDIT.READ'
  )
  AND NOT EXISTS
  (
      SELECT 1
      FROM [identity].RolePermissions AS rp
      WHERE rp.RoleId = r.RoleId
        AND rp.PermissionId = p.PermissionId
  );
GO


/*
    ============================================================
    ADMINISTRATOR permissions
    ============================================================
*/

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
WHERE r.Code = N'ADMINISTRATOR'
  AND NOT EXISTS
  (
      SELECT 1
      FROM [identity].RolePermissions AS rp
      WHERE rp.RoleId = r.RoleId
        AND rp.PermissionId = p.PermissionId
  );
GO


/*
    ============================================================
    SECURITY_ADMINISTRATOR permissions

    Deliberately narrower than ADMINISTRATOR.
    ============================================================
*/

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
WHERE r.Code = N'SECURITY_ADMINISTRATOR'
  AND p.Code IN
  (
      N'PROFILE.READ_SELF',
      N'IDENTITY.USER_MANAGE',
      N'IDENTITY.ROLE_MANAGE',
      N'AUDIT.READ'
  )
  AND NOT EXISTS
  (
      SELECT 1
      FROM [identity].RolePermissions AS rp
      WHERE rp.RoleId = r.RoleId
        AND rp.PermissionId = p.PermissionId
  );
GO


/*
    ============================================================
    Stored procedures
    ============================================================
*/


CREATE OR ALTER PROCEDURE [identity].User_GetPermissions
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        p.Code
    FROM [identity].UserRoles AS ur
    INNER JOIN [identity].RolePermissions AS rp
        ON rp.RoleId = ur.RoleId
    INNER JOIN [identity].Permissions AS p
        ON p.PermissionId = rp.PermissionId
    WHERE ur.UserId = @UserId
    ORDER BY p.Code;
END;
GO


CREATE OR ALTER PROCEDURE [identity].User_HasPermission
    @UserId UNIQUEIDENTIFIER,
    @PermissionCode NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CAST
        (
            CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM [identity].UserRoles AS ur
                    INNER JOIN [identity].RolePermissions AS rp
                        ON rp.RoleId = ur.RoleId
                    INNER JOIN [identity].Permissions AS p
                        ON p.PermissionId = rp.PermissionId
                    INNER JOIN [identity].Users AS u
                        ON u.UserId = ur.UserId
                    WHERE ur.UserId = @UserId
                      AND p.Code = @PermissionCode
                      AND u.IsActive = 1
                )
                THEN 1
                ELSE 0
            END
            AS BIT
        ) AS HasPermission;
END;
GO
