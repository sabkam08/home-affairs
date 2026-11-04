USE [HomeAffairsDb];
GO

/*
    ============================================================
    Citizen Contact Details

    Contact details belong to the Citizen Registry.

    They are deliberately separate from the authentication email
    stored in [identity].Users.

    Supported values:
        EMAIL
        MOBILE
        LANDLINE
        ALTERNATIVE
    ============================================================
*/

IF OBJECT_ID(
    N'[citizens].ContactDetails',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [citizens].ContactDetails
    (
        ContactDetailId UNIQUEIDENTIFIER NOT NULL,

        CitizenId UNIQUEIDENTIFIER NOT NULL,

        ContactType NVARCHAR(30) NOT NULL,

        ContactValue NVARCHAR(320) NOT NULL,

        IsPrimary BIT NOT NULL
            CONSTRAINT DF_ContactDetails_IsPrimary
            DEFAULT 0,

        IsVerified BIT NOT NULL
            CONSTRAINT DF_ContactDetails_IsVerified
            DEFAULT 0,

        IsActive BIT NOT NULL
            CONSTRAINT DF_ContactDetails_IsActive
            DEFAULT 1,

        CreatedAtUtc DATETIME2(7) NOT NULL,

        UpdatedAtUtc DATETIME2(7) NULL,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_ContactDetails
            PRIMARY KEY CLUSTERED (
                ContactDetailId
            ),

        CONSTRAINT FK_ContactDetails_Citizen
            FOREIGN KEY (CitizenId)
            REFERENCES [citizens].Citizens (
                CitizenId
            ),

        CONSTRAINT CK_ContactDetails_Type
            CHECK
            (
                ContactType IN
                (
                    N'EMAIL',
                    N'MOBILE',
                    N'LANDLINE',
                    N'ALTERNATIVE'
                )
            ),

        CONSTRAINT CK_ContactDetails_Value_NotEmpty
            CHECK
            (
                LEN(
                    LTRIM(
                        RTRIM(ContactValue)
                    )
                ) > 0
            )
    );

    /*
        Prevent duplicate active contact details for the
        same Citizen.

        Example:
        A Citizen cannot have the same mobile number saved
        twice as an active MOBILE contact.
    */
    CREATE UNIQUE NONCLUSTERED INDEX
        UX_ContactDetails_Citizen_Type_Value
    ON [citizens].ContactDetails
    (
        CitizenId,
        ContactType,
        ContactValue
    )
    WHERE IsActive = 1;

    /*
        A Citizen may have only one active primary contact
        per contact type.
    */
    CREATE UNIQUE NONCLUSTERED INDEX
        UX_ContactDetails_Citizen_PrimaryType
    ON [citizens].ContactDetails
    (
        CitizenId,
        ContactType
    )
    WHERE IsPrimary = 1
      AND IsActive = 1;

    CREATE NONCLUSTERED INDEX
        IX_ContactDetails_CitizenId
    ON [citizens].ContactDetails
    (
        CitizenId,
        IsActive
    );
END;
GO


/*
    ============================================================
    ContactDetail_GetByCitizenId
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [citizens].ContactDetail_GetByCitizenId
    @CitizenId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cd.ContactDetailId,
        cd.CitizenId,
        cd.ContactType,
        cd.ContactValue,
        cd.IsPrimary,
        cd.IsVerified,
        cd.IsActive,
        cd.CreatedAtUtc,
        cd.UpdatedAtUtc,
        cd.RowVersion
    FROM [citizens].ContactDetails AS cd
    WHERE cd.CitizenId = @CitizenId
      AND cd.IsActive = 1
    ORDER BY
        cd.ContactType,
        cd.IsPrimary DESC,
        cd.CreatedAtUtc;
END;
GO


/*
    ============================================================
    ContactDetail_Create

    If the new record is marked primary, the current primary
    record of the same contact type is demoted atomically.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [citizens].ContactDetail_Create
    @ContactDetailId UNIQUEIDENTIFIER,
    @CitizenId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @ContactType NVARCHAR(30),
    @ContactValue NVARCHAR(320),
    @IsPrimary BIT,
    @CreatedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [citizens].Citizens
            WHERE CitizenId = @CitizenId
              AND UserId = @UserId
              AND IsActive = 1
        )
        BEGIN;
            THROW 52101,
                'The citizen profile could not be found.',
                1;
        END;

        IF @ContactType NOT IN
        (
            N'EMAIL',
            N'MOBILE',
            N'LANDLINE',
            N'ALTERNATIVE'
        )
        BEGIN;
            THROW 52102,
                'The contact type is not valid.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [citizens].ContactDetails
                WITH (UPDLOCK, HOLDLOCK)
            WHERE CitizenId = @CitizenId
              AND ContactType = @ContactType
              AND ContactValue = @ContactValue
              AND IsActive = 1
        )
        BEGIN;
            THROW 52103,
                'This contact detail already exists.',
                1;
        END;

        /*
            If no active record exists for this type,
            the first contact automatically becomes primary.
        */
        IF NOT EXISTS
        (
            SELECT 1
            FROM [citizens].ContactDetails
            WHERE CitizenId = @CitizenId
              AND ContactType = @ContactType
              AND IsActive = 1
        )
        BEGIN
            SET @IsPrimary = 1;
        END;

        /*
            Demote the existing primary record of this type
            when the new record is explicitly primary.
        */
        IF @IsPrimary = 1
        BEGIN
            UPDATE [citizens].ContactDetails
            SET
                IsPrimary = 0,
                UpdatedAtUtc = @CreatedAtUtc
            WHERE CitizenId = @CitizenId
              AND ContactType = @ContactType
              AND IsPrimary = 1
              AND IsActive = 1;
        END;

        INSERT INTO [citizens].ContactDetails
        (
            ContactDetailId,
            CitizenId,
            ContactType,
            ContactValue,
            IsPrimary,
            IsVerified,
            IsActive,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            @ContactDetailId,
            @CitizenId,
            @ContactType,
            @ContactValue,
            @IsPrimary,
            0,
            1,
            @CreatedAtUtc,
            NULL
        );

        COMMIT TRANSACTION;

        SELECT
            cd.ContactDetailId,
            cd.CitizenId,
            cd.ContactType,
            cd.ContactValue,
            cd.IsPrimary,
            cd.IsVerified,
            cd.IsActive,
            cd.CreatedAtUtc,
            cd.UpdatedAtUtc,
            cd.RowVersion
        FROM [citizens].ContactDetails AS cd
        WHERE cd.ContactDetailId =
            @ContactDetailId;
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
