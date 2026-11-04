USE [HomeAffairsDb];
GO

/*
    ============================================================
    ContactDetail_GetByIdForUser

    Returns a contact only when it belongs to the Citizen
    profile associated with the supplied Identity user.

    This prevents cross-citizen contact access.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [citizens].ContactDetail_GetByIdForUser
    @ContactDetailId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
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
    INNER JOIN [citizens].Citizens AS c
        ON c.CitizenId = cd.CitizenId
    WHERE cd.ContactDetailId = @ContactDetailId
      AND c.UserId = @UserId
      AND c.IsActive = 1
      AND cd.IsActive = 1;
END;
GO


/*
    ============================================================
    ContactDetail_Update

    Optimistic concurrency is enforced by RowVersion.

    When the contact becomes primary, any existing active
    primary contact of the same type is demoted within the
    same transaction.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [citizens].ContactDetail_Update
    @ContactDetailId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @ContactType NVARCHAR(30),
    @ContactValue NVARCHAR(320),
    @IsPrimary BIT,
    @ExpectedRowVersion BINARY(8),
    @UpdatedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CitizenId UNIQUEIDENTIFIER;
        DECLARE @ExistingContactType NVARCHAR(30);

        SELECT
            @CitizenId = cd.CitizenId,
            @ExistingContactType = cd.ContactType
        FROM [citizens].ContactDetails AS cd
        INNER JOIN [citizens].Citizens AS c
            ON c.CitizenId = cd.CitizenId
        WHERE cd.ContactDetailId = @ContactDetailId
          AND c.UserId = @UserId
          AND c.IsActive = 1
          AND cd.IsActive = 1;

        IF @CitizenId IS NULL
        BEGIN;
            THROW 52104,
                'The contact detail could not be found.',
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
              AND ContactDetailId <> @ContactDetailId
              AND IsActive = 1
        )
        BEGIN;
            THROW 52103,
                'This contact detail already exists.',
                1;
        END;

        /*
            If this record is being made primary,
            demote the current primary for the target type.

            The target record is explicitly excluded.
        */
        IF @IsPrimary = 1
        BEGIN
            UPDATE [citizens].ContactDetails
            SET
                IsPrimary = 0,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE CitizenId = @CitizenId
              AND ContactType = @ContactType
              AND ContactDetailId <> @ContactDetailId
              AND IsPrimary = 1
              AND IsActive = 1;
        END;

        UPDATE [citizens].ContactDetails
        SET
            ContactType = @ContactType,
            ContactValue = @ContactValue,
            IsPrimary = @IsPrimary,
            UpdatedAtUtc = @UpdatedAtUtc
        WHERE ContactDetailId = @ContactDetailId
          AND CitizenId = @CitizenId
          AND IsActive = 1
          AND RowVersion = @ExpectedRowVersion;

        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 52105,
                'The contact detail was modified by another request.',
                1;
        END;

        /*
            Ensure that the type from which this contact moved
            still has a primary contact if active records remain.

            This matters when a primary EMAIL becomes MOBILE,
            for example.
        */
        IF @ExistingContactType <> @ContactType
           AND NOT EXISTS
        (
            SELECT 1
            FROM [citizens].ContactDetails
            WHERE CitizenId = @CitizenId
              AND ContactType = @ExistingContactType
              AND IsPrimary = 1
              AND IsActive = 1
        )
        BEGIN
            DECLARE @OldTypeReplacementId UNIQUEIDENTIFIER;

            SELECT TOP (1)
                @OldTypeReplacementId = ContactDetailId
            FROM [citizens].ContactDetails
            WHERE CitizenId = @CitizenId
              AND ContactType = @ExistingContactType
              AND IsActive = 1
            ORDER BY
                CreatedAtUtc,
                ContactDetailId;

            IF @OldTypeReplacementId IS NOT NULL
            BEGIN
                UPDATE [citizens].ContactDetails
                SET
                    IsPrimary = 1,
                    UpdatedAtUtc = @UpdatedAtUtc
                WHERE ContactDetailId =
                    @OldTypeReplacementId;
            END;
        END;

        /*
            If the updated type currently has active records
            but none is primary, promote the updated record.

            This prevents a contact type from accidentally
            ending up without a primary record.
        */
        IF NOT EXISTS
        (
            SELECT 1
            FROM [citizens].ContactDetails
            WHERE CitizenId = @CitizenId
              AND ContactType = @ContactType
              AND IsPrimary = 1
              AND IsActive = 1
        )
        BEGIN
            UPDATE [citizens].ContactDetails
            SET
                IsPrimary = 1,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE ContactDetailId = @ContactDetailId;
        END;

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


/*
    ============================================================
    ContactDetail_Delete

    Contacts are soft-deleted.

    If the deleted contact was primary, the oldest remaining
    active contact of the same type becomes primary.

    RowVersion prevents stale clients from deleting a contact
    that has changed since they retrieved it.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [citizens].ContactDetail_Delete
    @ContactDetailId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @ExpectedRowVersion BINARY(8),
    @DeletedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CitizenId UNIQUEIDENTIFIER;
        DECLARE @ContactType NVARCHAR(30);
        DECLARE @WasPrimary BIT;

        SELECT
            @CitizenId = cd.CitizenId,
            @ContactType = cd.ContactType,
            @WasPrimary = cd.IsPrimary
        FROM [citizens].ContactDetails AS cd
        INNER JOIN [citizens].Citizens AS c
            ON c.CitizenId = cd.CitizenId
        WHERE cd.ContactDetailId = @ContactDetailId
          AND c.UserId = @UserId
          AND c.IsActive = 1
          AND cd.IsActive = 1;

        IF @CitizenId IS NULL
        BEGIN;
            THROW 52104,
                'The contact detail could not be found.',
                1;
        END;

        UPDATE [citizens].ContactDetails
        SET
            IsPrimary = 0,
            IsActive = 0,
            UpdatedAtUtc = @DeletedAtUtc
        WHERE ContactDetailId = @ContactDetailId
          AND RowVersion = @ExpectedRowVersion
          AND IsActive = 1;

        IF @@ROWCOUNT = 0
        BEGIN;
            THROW 52105,
                'The contact detail was modified by another request.',
                1;
        END;

        IF @WasPrimary = 1
        BEGIN
            DECLARE @ReplacementContactDetailId
                UNIQUEIDENTIFIER;

            SELECT TOP (1)
                @ReplacementContactDetailId =
                    ContactDetailId
            FROM [citizens].ContactDetails
            WHERE CitizenId = @CitizenId
              AND ContactType = @ContactType
              AND IsActive = 1
            ORDER BY
                CreatedAtUtc,
                ContactDetailId;

            IF @ReplacementContactDetailId IS NOT NULL
            BEGIN
                UPDATE [citizens].ContactDetails
                SET
                    IsPrimary = 1,
                    UpdatedAtUtc = @DeletedAtUtc
                WHERE ContactDetailId =
                    @ReplacementContactDetailId;
            END;
        END;

        COMMIT TRANSACTION;
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
