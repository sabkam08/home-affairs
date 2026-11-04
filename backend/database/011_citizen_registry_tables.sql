USE [HomeAffairsDb];
GO

/*
    ============================================================
    Citizen Registry
    Core Citizen Profile

    A Citizen does not necessarily require a user account.

    Examples:
    - Adult self-service citizen:
        Citizen.UserId -> [identity].Users.UserId

    - Child represented by parent/guardian:
        Citizen.UserId = NULL

    Applicant/Subject relationships will be introduced
    separately rather than embedding that concept here.
    ============================================================
*/

IF OBJECT_ID(N'[citizens].Citizens', N'U') IS NULL
BEGIN
    CREATE TABLE [citizens].Citizens
    (
        CitizenId UNIQUEIDENTIFIER NOT NULL,

        /*
            Optional because not every subject represented
            by the system necessarily owns an application
            login account.
        */
        UserId UNIQUEIDENTIFIER NULL,

        SouthAfricanIdNumber NVARCHAR(13) NULL,

        FirstName NVARCHAR(100) NOT NULL,

        MiddleNames NVARCHAR(200) NULL,

        LastName NVARCHAR(100) NOT NULL,

        DateOfBirth DATE NOT NULL,

        /*
            Single-character persisted values:

            M = Male
            F = Female
            X = Unspecified/Other

            We are not treating this value as authorization
            or identity-account information.
        */
        Sex CHAR(1) NOT NULL,

        IsActive BIT NOT NULL
            CONSTRAINT DF_Citizens_IsActive
            DEFAULT 1,

        CreatedAtUtc DATETIME2(7) NOT NULL,

        UpdatedAtUtc DATETIME2(7) NULL,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_Citizens
            PRIMARY KEY CLUSTERED (CitizenId),

        CONSTRAINT FK_Citizens_User
            FOREIGN KEY (UserId)
            REFERENCES [identity].Users (UserId),

        CONSTRAINT CK_Citizens_FirstName_NotEmpty
            CHECK (
                LEN(LTRIM(RTRIM(FirstName))) > 0
            ),

        CONSTRAINT CK_Citizens_LastName_NotEmpty
            CHECK (
                LEN(LTRIM(RTRIM(LastName))) > 0
            ),

        CONSTRAINT CK_Citizens_Sex
            CHECK (
                Sex IN ('M', 'F', 'X')
            ),

        CONSTRAINT CK_Citizens_SouthAfricanIdNumber
            CHECK
            (
                SouthAfricanIdNumber IS NULL
                OR
                (
                    LEN(SouthAfricanIdNumber) = 13
                    AND
                    SouthAfricanIdNumber NOT LIKE '%[^0-9]%'
                )
            )
    );

    /*
        A single Identity account may own at most one
        Citizen profile.
    */
    CREATE UNIQUE NONCLUSTERED INDEX
        UX_Citizens_UserId
    ON [citizens].Citizens (UserId)
    WHERE UserId IS NOT NULL;

    /*
        A South African ID number may belong to only one
        citizen record when supplied.
    */
    CREATE UNIQUE NONCLUSTERED INDEX
        UX_Citizens_SouthAfricanIdNumber
    ON [citizens].Citizens (SouthAfricanIdNumber)
    WHERE SouthAfricanIdNumber IS NOT NULL;

    CREATE NONCLUSTERED INDEX
        IX_Citizens_LastName_FirstName
    ON [citizens].Citizens
    (
        LastName,
        FirstName
    );
END;
GO
