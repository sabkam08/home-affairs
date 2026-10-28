USE [HomeAffairsDb];
GO

IF SCHEMA_ID(N'identity') IS NULL
    EXEC(N'CREATE SCHEMA [identity]');
GO

IF SCHEMA_ID(N'citizens') IS NULL
    EXEC(N'CREATE SCHEMA [citizens]');
GO

IF SCHEMA_ID(N'applications') IS NULL
    EXEC(N'CREATE SCHEMA [applications]');
GO

IF SCHEMA_ID(N'workflows') IS NULL
    EXEC(N'CREATE SCHEMA [workflows]');
GO

IF SCHEMA_ID(N'documents') IS NULL
    EXEC(N'CREATE SCHEMA [documents]');
GO

IF SCHEMA_ID(N'birthcertificates') IS NULL
    EXEC(N'CREATE SCHEMA [birthcertificates]');
GO

IF SCHEMA_ID(N'smartids') IS NULL
    EXEC(N'CREATE SCHEMA [smartids]');
GO

IF SCHEMA_ID(N'auditing') IS NULL
    EXEC(N'CREATE SCHEMA [auditing]');
GO

IF SCHEMA_ID(N'messaging') IS NULL
    EXEC(N'CREATE SCHEMA [messaging]');
GO
