USE [HomeAffairsDb];
GO

/*
    ============================================================
    CitizenRelationship_Update

    Updates the relationship type for an active relationship
    owned by the authenticated representative.

    Optimistic concurrency is enforced through RowVersion.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [citizens].CitizenRelationship_Update
    @SubjectCitizenId UNIQUEIDENTIFIER,
    @RepresentativeUserId UNIQUEIDENTIFIER,
    @RelationshipType NVARCHAR(40),
    @ExpectedRowVersion BINARY(8),
    @UpdatedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

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

    DECLARE @CitizenRelationshipId
        UNIQUEIDENTIFIER;

    SELECT
        @CitizenRelationshipId =
            cr.CitizenRelationshipId
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

    IF @CitizenRelationshipId IS NULL
    BEGIN;
        THROW 52307,
            'The citizen relationship could not be found.',
            1;
    END;

    UPDATE [citizens].CitizenRelationships
    SET
        RelationshipType =
            @RelationshipType,

        UpdatedAtUtc =
            @UpdatedAtUtc

    WHERE CitizenRelationshipId =
        @CitizenRelationshipId
      AND RowVersion =
        @ExpectedRowVersion
      AND IsActive = 1;

    IF @@ROWCOUNT = 0
    BEGIN;
        THROW 52308,
            'The citizen relationship was modified by another request.',
            1;
    END;

    EXEC
        [citizens].CitizenRelationship_GetSubjectForRepresentative
        @SubjectCitizenId =
            @SubjectCitizenId,
        @RepresentativeUserId =
            @RepresentativeUserId;
END;
GO


/*
    ============================================================
    CitizenRelationship_Deactivate

    Soft-deactivates the relationship.

    The represented Citizen is retained and remains active.
    This preserves the subject for auditing and historical
    application references.

    Optimistic concurrency is enforced through RowVersion.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [citizens].CitizenRelationship_Deactivate
    @SubjectCitizenId UNIQUEIDENTIFIER,
    @RepresentativeUserId UNIQUEIDENTIFIER,
    @ExpectedRowVersion BINARY(8),
    @DeactivatedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CitizenRelationshipId
        UNIQUEIDENTIFIER;

    SELECT
        @CitizenRelationshipId =
            cr.CitizenRelationshipId
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

    IF @CitizenRelationshipId IS NULL
    BEGIN;
        THROW 52307,
            'The citizen relationship could not be found.',
            1;
    END;

    UPDATE [citizens].CitizenRelationships
    SET
        IsActive = 0,
        UpdatedAtUtc =
            @DeactivatedAtUtc
    WHERE CitizenRelationshipId =
        @CitizenRelationshipId
      AND RowVersion =
        @ExpectedRowVersion
      AND IsActive = 1;

    IF @@ROWCOUNT = 0
    BEGIN;
        THROW 52308,
            'The citizen relationship was modified by another request.',
            1;
    END;
END;
GO
