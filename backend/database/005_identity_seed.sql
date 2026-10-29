USE [HomeAffairsDb];
GO

DECLARE @NowUtc DATETIME2(7) = SYSUTCDATETIME();

IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Roles
    WHERE Code = N'CITIZEN'
)
BEGIN
    INSERT INTO [identity].Roles
    (
        RoleId,
        Code,
        Name,
        Description,
        IsSystemRole,
        CreatedAtUtc
    )
    VALUES
    (
        NEWID(),
        N'CITIZEN',
        N'Citizen',
        N'A citizen using Home Affairs self-service functions.',
        1,
        @NowUtc
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Roles
    WHERE Code = N'PRACTITIONER'
)
BEGIN
    INSERT INTO [identity].Roles
    (
        RoleId,
        Code,
        Name,
        Description,
        IsSystemRole,
        CreatedAtUtc
    )
    VALUES
    (
        NEWID(),
        N'PRACTITIONER',
        N'Practitioner',
        N'A Home Affairs practitioner who processes applications.',
        1,
        SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Roles
    WHERE Code = N'SUPERVISOR'
)
BEGIN
    INSERT INTO [identity].Roles
    (
        RoleId,
        Code,
        Name,
        Description,
        IsSystemRole,
        CreatedAtUtc
    )
    VALUES
    (
        NEWID(),
        N'SUPERVISOR',
        N'Supervisor',
        N'A supervisor responsible for review and approval operations.',
        1,
        SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Roles
    WHERE Code = N'ADMINISTRATOR'
)
BEGIN
    INSERT INTO [identity].Roles
    (
        RoleId,
        Code,
        Name,
        Description,
        IsSystemRole,
        CreatedAtUtc
    )
    VALUES
    (
        NEWID(),
        N'ADMINISTRATOR',
        N'Administrator',
        N'A system administrator responsible for application administration.',
        1,
        SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM [identity].Roles
    WHERE Code = N'SECURITY_ADMINISTRATOR'
)
BEGIN
    INSERT INTO [identity].Roles
    (
        RoleId,
        Code,
        Name,
        Description,
        IsSystemRole,
        CreatedAtUtc
    )
    VALUES
    (
        NEWID(),
        N'SECURITY_ADMINISTRATOR',
        N'Security Administrator',
        N'An administrator responsible for security configuration and review.',
        1,
        SYSUTCDATETIME()
    );
END;
GO
