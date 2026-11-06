USE [HomeAffairsDb];
GO

/*
    ============================================================
    Citizen Relationships

    The RepresentativeCitizenId identifies the adult Citizen
    who may act as an applicant.

    The SubjectCitizenId identifies the represented child.

    The child may have UserId = NULL because a represented
    subject does not require an Identity account.
    ============================================================
*/

IF OBJECT_ID(
    N'[citizens].CitizenRelationships',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [citizens].CitizenRelationships
    (
        CitizenRelationshipId UNIQUEIDENTIFIER NOT NULL,

        RepresentativeCitizenId UNIQUEIDENTIFIER NOT NULL,

        SubjectCitizenId UNIQUEIDENTIFIER NOT NULL,

        RelationshipType NVARCHAR(40) NOT NULL,

        IsActive BIT NOT NULL
            CONSTRAINT DF_CitizenRelationships_IsActive
            DEFAULT 1,

        CreatedAtUtc DATETIME2(7) NOT NULL,

        UpdatedAtUtc DATETIME2(7) NULL,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_CitizenRelationships
            PRIMARY KEY CLUSTERED (
                CitizenRelationshipId
            ),

        CONSTRAINT FK_CitizenRelationships_Representative
            FOREIGN KEY (
                RepresentativeCitizenId
            )
            REFERENCES [citizens].Citizens (
                CitizenId
            ),

        CONSTRAINT FK_CitizenRelationships_Subject
            FOREIGN KEY (
                SubjectCitizenId
            )
            REFERENCES [citizens].Citizens (
                CitizenId
            ),

        CONSTRAINT CK_CitizenRelationships_Type
            CHECK
            (
                RelationshipType IN
                (
                    N'BIOLOGICAL_PARENT',
                    N'ADOPTIVE_PARENT',
                    N'LEGAL_GUARDIAN'
                )
            ),

        CONSTRAINT CK_CitizenRelationships_NotSelf
            CHECK
            (
                RepresentativeCitizenId <>
                SubjectCitizenId
            )
    );

    CREATE UNIQUE NONCLUSTERED INDEX
        UX_CitizenRelationships_ActiveRelationship
    ON [citizens].CitizenRelationships
    (
        RepresentativeCitizenId,
        SubjectCitizenId,
        RelationshipType
    )
    WHERE IsActive = 1;

    CREATE NONCLUSTERED INDEX
        IX_CitizenRelationships_Representative
    ON [citizens].CitizenRelationships
    (
        RepresentativeCitizenId,
        IsActive
    );

    CREATE NONCLUSTERED INDEX
        IX_CitizenRelationships_Subject
    ON [citizens].CitizenRelationships
    (
        SubjectCitizenId,
        IsActive
    );
END;
GO


/*
    ============================================================
    Citizen_CreateRepresentedMinor

    Creates:
    1. A child Citizen without a UserId.
    2. The relationship between the authenticated adult
       Citizen and the child.

    Both records are committed atomically.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [citizens].Citizen_CreateRepresentedMinor
    @SubjectCitizenId UNIQUEIDENTIFIER,
    @CitizenRelationshipId UNIQUEIDENTIFIER,
    @RepresentativeUserId UNIQUEIDENTIFIER,

    @SouthAfricanIdNumber NVARCHAR(13) = NULL,
    @FirstName NVARCHAR(100),
    @MiddleNames NVARCHAR(200) = NULL,
    @LastName NVARCHAR(100),
    @DateOfBirth DATE,
    @Sex CHAR(1),

    @RelationshipType NVARCHAR(40),
    @CreatedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @RepresentativeCitizenId
            UNIQUEIDENTIFIER;

        DECLARE @RepresentativeDateOfBirth
            DATE;

        SELECT
            @RepresentativeCitizenId =
                c.CitizenId,

            @RepresentativeDateOfBirth =
                c.DateOfBirth

        FROM [citizens].Citizens AS c
        WHERE c.UserId =
            @RepresentativeUserId
          AND c.IsActive = 1;

        IF @RepresentativeCitizenId IS NULL
        BEGIN;
            THROW 52301,
                'The representative citizen profile could not be found.',
                1;
        END;

        /*
            The representative must be at least 18 as at
            the relationship creation date.
        */
        IF DATEADD(
                YEAR,
                18,
                @RepresentativeDateOfBirth
            ) > CAST(@CreatedAtUtc AS DATE)
        BEGIN;
            THROW 52302,
                'The representative citizen must be at least 18 years old.',
                1;
        END;

        /*
            The represented Citizen must be under 18.
        */
        IF DATEADD(
                YEAR,
                18,
                @DateOfBirth
            ) <= CAST(@CreatedAtUtc AS DATE)
        BEGIN;
            THROW 52303,
                'The represented citizen must be under 18 years old.',
                1;
        END;

        IF @DateOfBirth >
            CAST(@CreatedAtUtc AS DATE)
        BEGIN;
            THROW 52304,
                'Date of birth cannot be in the future.',
                1;
        END;

        IF @RelationshipType NOT IN
        (
            N'BIOLOGICAL_PARENT',
            N'ADOPTIVE_PARENT',
            N'LEGAL_GUARDIAN'
        )
        BEGIN;
            THROW 52305,
                'The relationship type is not valid.',
                1;
        END;

        IF @SouthAfricanIdNumber IS NOT NULL
           AND EXISTS
        (
            SELECT 1
            FROM [citizens].Citizens
                WITH (UPDLOCK, HOLDLOCK)
            WHERE SouthAfricanIdNumber =
                @SouthAfricanIdNumber
        )
        BEGIN;
            THROW 52306,
                'A citizen with this South African ID number already exists.',
                1;
        END;

        INSERT INTO [citizens].Citizens
        (
            CitizenId,
            UserId,
            SouthAfricanIdNumber,
            FirstName,
            MiddleNames,
            LastName,
            DateOfBirth,
            Sex,
            IsActive,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            @SubjectCitizenId,
            NULL,
            @SouthAfricanIdNumber,
            @FirstName,
            @MiddleNames,
            @LastName,
            @DateOfBirth,
            @Sex,
            1,
            @CreatedAtUtc,
            NULL
        );

        INSERT INTO [citizens].CitizenRelationships
        (
            CitizenRelationshipId,
            RepresentativeCitizenId,
            SubjectCitizenId,
            RelationshipType,
            IsActive,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            @CitizenRelationshipId,
            @RepresentativeCitizenId,
            @SubjectCitizenId,
            @RelationshipType,
            1,
            @CreatedAtUtc,
            NULL
        );

        COMMIT TRANSACTION;

        SELECT
            cr.CitizenRelationshipId,
            cr.RepresentativeCitizenId,
            cr.SubjectCitizenId,
            cr.RelationshipType,
            cr.IsActive AS RelationshipIsActive,
            cr.CreatedAtUtc AS RelationshipCreatedAtUtc,
            cr.UpdatedAtUtc AS RelationshipUpdatedAtUtc,
            cr.RowVersion AS RelationshipRowVersion,

            subject.UserId AS SubjectUserId,
            subject.SouthAfricanIdNumber,
            subject.FirstName,
            subject.MiddleNames,
            subject.LastName,
            subject.DateOfBirth,
            subject.Sex,
            subject.IsActive AS SubjectIsActive,
            subject.CreatedAtUtc AS SubjectCreatedAtUtc,
            subject.UpdatedAtUtc AS SubjectUpdatedAtUtc,
            subject.RowVersion AS SubjectRowVersion

        FROM [citizens].CitizenRelationships AS cr
        INNER JOIN [citizens].Citizens AS subject
            ON subject.CitizenId =
                cr.SubjectCitizenId
        WHERE cr.CitizenRelationshipId =
            @CitizenRelationshipId;
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
    CitizenRelationship_GetByRepresentativeUserId
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [citizens].CitizenRelationship_GetByRepresentativeUserId
    @RepresentativeUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cr.CitizenRelationshipId,
        cr.RepresentativeCitizenId,
        cr.SubjectCitizenId,
        cr.RelationshipType,
        cr.IsActive AS RelationshipIsActive,
        cr.CreatedAtUtc AS RelationshipCreatedAtUtc,
        cr.UpdatedAtUtc AS RelationshipUpdatedAtUtc,
        cr.RowVersion AS RelationshipRowVersion,

        subject.UserId AS SubjectUserId,
        subject.SouthAfricanIdNumber,
        subject.FirstName,
        subject.MiddleNames,
        subject.LastName,
        subject.DateOfBirth,
        subject.Sex,
        subject.IsActive AS SubjectIsActive,
        subject.CreatedAtUtc AS SubjectCreatedAtUtc,
        subject.UpdatedAtUtc AS SubjectUpdatedAtUtc,
        subject.RowVersion AS SubjectRowVersion

    FROM [citizens].CitizenRelationships AS cr
    INNER JOIN [citizens].Citizens AS representative
        ON representative.CitizenId =
            cr.RepresentativeCitizenId
    INNER JOIN [citizens].Citizens AS subject
        ON subject.CitizenId =
            cr.SubjectCitizenId

    WHERE representative.UserId =
        @RepresentativeUserId
      AND representative.IsActive = 1
      AND subject.IsActive = 1
      AND cr.IsActive = 1

    ORDER BY
        subject.LastName,
        subject.FirstName,
        subject.DateOfBirth;
END;
GO


/*
    ============================================================
    CitizenRelationship_GetSubjectForRepresentative

    Returns the represented subject only when the authenticated
    user owns an active relationship to that subject.

    This supports 404 information hiding.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [citizens].CitizenRelationship_GetSubjectForRepresentative
    @SubjectCitizenId UNIQUEIDENTIFIER,
    @RepresentativeUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cr.CitizenRelationshipId,
        cr.RepresentativeCitizenId,
        cr.SubjectCitizenId,
        cr.RelationshipType,
        cr.IsActive AS RelationshipIsActive,
        cr.CreatedAtUtc AS RelationshipCreatedAtUtc,
        cr.UpdatedAtUtc AS RelationshipUpdatedAtUtc,
        cr.RowVersion AS RelationshipRowVersion,

        subject.UserId AS SubjectUserId,
        subject.SouthAfricanIdNumber,
        subject.FirstName,
        subject.MiddleNames,
        subject.LastName,
        subject.DateOfBirth,
        subject.Sex,
        subject.IsActive AS SubjectIsActive,
        subject.CreatedAtUtc AS SubjectCreatedAtUtc,
        subject.UpdatedAtUtc AS SubjectUpdatedAtUtc,
        subject.RowVersion AS SubjectRowVersion

    FROM [citizens].CitizenRelationships AS cr
    INNER JOIN [citizens].Citizens AS representative
        ON representative.CitizenId =
            cr.RepresentativeCitizenId
    INNER JOIN [citizens].Citizens AS subject
        ON subject.CitizenId =
            cr.SubjectCitizenId

    WHERE cr.SubjectCitizenId =
        @SubjectCitizenId
      AND representative.UserId =
        @RepresentativeUserId
      AND representative.IsActive = 1
      AND subject.IsActive = 1
      AND cr.IsActive = 1;
END;
GO
