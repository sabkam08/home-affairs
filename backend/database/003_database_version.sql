USE [HomeAffairsDb];
GO

IF OBJECT_ID(
    N'dbo.DatabaseVersion',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE dbo.DatabaseVersion
    (
        DatabaseVersionId INT IDENTITY(1, 1) NOT NULL,
        ScriptName NVARCHAR(255) NOT NULL,
        AppliedAtUtc DATETIME2(7) NOT NULL
            CONSTRAINT DF_DatabaseVersion_AppliedAtUtc
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_DatabaseVersion
            PRIMARY KEY CLUSTERED (DatabaseVersionId),

        CONSTRAINT UQ_DatabaseVersion_ScriptName
            UNIQUE (ScriptName)
    );
END;
GO
