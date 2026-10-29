USE [HomeAffairsDb];
GO

IF OBJECT_ID(N'[identity].Users', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].Users
    (
        UserId UNIQUEIDENTIFIER NOT NULL,
        Email NVARCHAR(320) NOT NULL,
        NormalizedEmail NVARCHAR(320) NOT NULL,
        PasswordHash NVARCHAR(1000) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_Users_IsActive
            DEFAULT 1,
        EmailVerifiedAtUtc DATETIME2(7) NULL,
        FailedLoginAttempts INT NOT NULL
            CONSTRAINT DF_Users_FailedLoginAttempts
            DEFAULT 0,
        LockedUntilUtc DATETIME2(7) NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        UpdatedAtUtc DATETIME2(7) NULL,
        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_Users
            PRIMARY KEY CLUSTERED (UserId),

        CONSTRAINT CK_Users_Email_NotEmpty
            CHECK (LEN(LTRIM(RTRIM(Email))) > 0),

        CONSTRAINT CK_Users_NormalizedEmail_NotEmpty
            CHECK (LEN(LTRIM(RTRIM(NormalizedEmail))) > 0),

        CONSTRAINT CK_Users_FirstName_NotEmpty
            CHECK (LEN(LTRIM(RTRIM(FirstName))) > 0),

        CONSTRAINT CK_Users_LastName_NotEmpty
            CHECK (LEN(LTRIM(RTRIM(LastName))) > 0),

        CONSTRAINT CK_Users_FailedLoginAttempts
            CHECK (FailedLoginAttempts >= 0)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_Users_NormalizedEmail
        ON [identity].Users (NormalizedEmail);
END;
GO

IF OBJECT_ID(N'[identity].Roles', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].Roles
    (
        RoleId UNIQUEIDENTIFIER NOT NULL,
        Code NVARCHAR(100) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsSystemRole BIT NOT NULL
            CONSTRAINT DF_Roles_IsSystemRole
            DEFAULT 1,
        CreatedAtUtc DATETIME2(7) NOT NULL,

        CONSTRAINT PK_Roles
            PRIMARY KEY CLUSTERED (RoleId),

        CONSTRAINT UQ_Roles_Code
            UNIQUE (Code)
    );
END;
GO

IF OBJECT_ID(N'[identity].UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].UserRoles
    (
        UserId UNIQUEIDENTIFIER NOT NULL,
        RoleId UNIQUEIDENTIFIER NOT NULL,
        AssignedAtUtc DATETIME2(7) NOT NULL,
        AssignedByUserId UNIQUEIDENTIFIER NULL,

        CONSTRAINT PK_UserRoles
            PRIMARY KEY CLUSTERED (UserId, RoleId),

        CONSTRAINT FK_UserRoles_User
            FOREIGN KEY (UserId)
            REFERENCES [identity].Users (UserId),

        CONSTRAINT FK_UserRoles_Role
            FOREIGN KEY (RoleId)
            REFERENCES [identity].Roles (RoleId),

        CONSTRAINT FK_UserRoles_AssignedByUser
            FOREIGN KEY (AssignedByUserId)
            REFERENCES [identity].Users (UserId)
    );
END;
GO

IF OBJECT_ID(N'[identity].RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].RefreshTokens
    (
        RefreshTokenId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        TokenHash VARBINARY(64) NOT NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        ExpiresAtUtc DATETIME2(7) NOT NULL,
        RevokedAtUtc DATETIME2(7) NULL,
        ReplacedByTokenId UNIQUEIDENTIFIER NULL,
        CreatedByIpAddress NVARCHAR(64) NULL,
        RevokedByIpAddress NVARCHAR(64) NULL,

        CONSTRAINT PK_RefreshTokens
            PRIMARY KEY CLUSTERED (RefreshTokenId),

        CONSTRAINT FK_RefreshTokens_User
            FOREIGN KEY (UserId)
            REFERENCES [identity].Users (UserId),

        CONSTRAINT FK_RefreshTokens_Replacement
            FOREIGN KEY (ReplacedByTokenId)
            REFERENCES [identity].RefreshTokens (RefreshTokenId),

        CONSTRAINT CK_RefreshTokens_Expiry
            CHECK (ExpiresAtUtc > CreatedAtUtc)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_RefreshTokens_TokenHash
        ON [identity].RefreshTokens (TokenHash);

    CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId
        ON [identity].RefreshTokens (UserId);
END;
GO

IF OBJECT_ID(N'[identity].SecurityEvents', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].SecurityEvents
    (
        SecurityEventId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NULL,
        EventType NVARCHAR(100) NOT NULL,
        Description NVARCHAR(1000) NULL,
        IpAddress NVARCHAR(64) NULL,
        UserAgent NVARCHAR(1000) NULL,
        CorrelationId NVARCHAR(100) NULL,
        OccurredAtUtc DATETIME2(7) NOT NULL,

        CONSTRAINT PK_SecurityEvents
            PRIMARY KEY CLUSTERED (SecurityEventId),

        CONSTRAINT FK_SecurityEvents_User
            FOREIGN KEY (UserId)
            REFERENCES [identity].Users (UserId)
    );

    CREATE NONCLUSTERED INDEX IX_SecurityEvents_UserId_OccurredAtUtc
        ON [identity].SecurityEvents (UserId, OccurredAtUtc DESC);

    CREATE NONCLUSTERED INDEX IX_SecurityEvents_EventType_OccurredAtUtc
        ON [identity].SecurityEvents (EventType, OccurredAtUtc DESC);
END;
GO
