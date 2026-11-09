USE [HomeAffairsDb];
GO

IF OBJECT_ID(
    N'[applications].ApplicationAssignments',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [applications].ApplicationAssignments
    (
        ApplicationAssignmentId UNIQUEIDENTIFIER NOT NULL,

        ApplicationId UNIQUEIDENTIFIER NOT NULL,

        AssignedToUserId UNIQUEIDENTIFIER NOT NULL,

        AssignedByUserId UNIQUEIDENTIFIER NOT NULL,

        AssignedAtUtc DATETIME2(7) NOT NULL,

        UnassignedAtUtc DATETIME2(7) NULL,

        UnassignedByUserId UNIQUEIDENTIFIER NULL,

        Reason NVARCHAR(500) NULL,

        IsActive BIT NOT NULL
            CONSTRAINT DF_ApplicationAssignments_IsActive
            DEFAULT 1,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_ApplicationAssignments
            PRIMARY KEY CLUSTERED (
                ApplicationAssignmentId
            ),

        CONSTRAINT FK_ApplicationAssignments_Application
            FOREIGN KEY (ApplicationId)
            REFERENCES [applications].Applications (
                ApplicationId
            ),

        CONSTRAINT FK_ApplicationAssignments_AssignedTo
            FOREIGN KEY (AssignedToUserId)
            REFERENCES [identity].Users (
                UserId
            ),

        CONSTRAINT FK_ApplicationAssignments_AssignedBy
            FOREIGN KEY (AssignedByUserId)
            REFERENCES [identity].Users (
                UserId
            ),

        CONSTRAINT FK_ApplicationAssignments_UnassignedBy
            FOREIGN KEY (UnassignedByUserId)
            REFERENCES [identity].Users (
                UserId
            )
    );

    /*
        Exactly one active practitioner assignment
        per application.
    */
    CREATE UNIQUE NONCLUSTERED INDEX
        UX_ApplicationAssignments_Active
    ON [applications].ApplicationAssignments
    (
        ApplicationId
    )
    WHERE IsActive = 1;

    CREATE NONCLUSTERED INDEX
        IX_ApplicationAssignments_AssignedTo
    ON [applications].ApplicationAssignments
    (
        AssignedToUserId,
        IsActive,
        AssignedAtUtc
    );
END;
GO


/*
    Returns true only when the target user is active and has
    PRACTITIONER or SUPERVISOR capability through their role.
*/
CREATE OR ALTER PROCEDURE
    [applications].Application_Assign
    @ApplicationId UNIQUEIDENTIFIER,
    @AssignedToUserId UNIQUEIDENTIFIER,
    @AssignedByUserId UNIQUEIDENTIFIER,
    @Reason NVARCHAR(500) = NULL,
    @AssignedAtUtc DATETIME2(7),
    @ExpectedApplicationRowVersion BINARY(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Status NVARCHAR(30);

        SELECT
            @Status = Status
        FROM [applications].Applications
            WITH (UPDLOCK, HOLDLOCK)
        WHERE ApplicationId = @ApplicationId;

        IF @Status IS NULL
        BEGIN;
            THROW 52501,
                'The application could not be found.',
                1;
        END;

        IF @Status NOT IN
        (
            N'SUBMITTED',
            N'IN_PROGRESS'
        )
        BEGIN;
            THROW 52502,
                'The application is not assignable in its current state.',
                1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [identity].Users AS u
            INNER JOIN [identity].UserRoles AS ur
                ON ur.UserId = u.UserId
            INNER JOIN [identity].Roles AS r
                ON r.RoleId = ur.RoleId
            WHERE u.UserId = @AssignedToUserId
              AND u.IsActive = 1
              AND r.Code IN
              (
                  N'PRACTITIONER',
                  N'SUPERVISOR'
              )
        )
        BEGIN;
            THROW 52503,
                'The target user is not an active practitioner or supervisor.',
                1;
        END;

        /*
            Protect assignment against a stale application view.
        */
        IF NOT EXISTS
        (
            SELECT 1
            FROM [applications].Applications
            WHERE ApplicationId = @ApplicationId
              AND RowVersion =
                  @ExpectedApplicationRowVersion
        )
        BEGIN;
            THROW 52504,
                'The application was modified by another request.',
                1;
        END;

        /*
            Close the previous assignment, if present.
        */
        UPDATE [applications].ApplicationAssignments
        SET
            IsActive = 0,
            UnassignedAtUtc = @AssignedAtUtc,
            UnassignedByUserId = @AssignedByUserId
        WHERE ApplicationId = @ApplicationId
          AND IsActive = 1;

        INSERT INTO [applications].ApplicationAssignments
        (
            ApplicationAssignmentId,
            ApplicationId,
            AssignedToUserId,
            AssignedByUserId,
            AssignedAtUtc,
            UnassignedAtUtc,
            UnassignedByUserId,
            Reason,
            IsActive
        )
        VALUES
        (
            NEWID(),
            @ApplicationId,
            @AssignedToUserId,
            @AssignedByUserId,
            @AssignedAtUtc,
            NULL,
            NULL,
            @Reason,
            1
        );

        /*
            First assignment moves SUBMITTED to IN_PROGRESS.
            Reassignment leaves IN_PROGRESS unchanged.
        */
        UPDATE [applications].Applications
        SET
            Status = N'IN_PROGRESS',
            UpdatedAtUtc = @AssignedAtUtc
        WHERE ApplicationId = @ApplicationId;

        IF @Status = N'SUBMITTED'
        BEGIN
            INSERT INTO
                [applications].ApplicationStatusHistory
            (
                ApplicationStatusHistoryId,
                ApplicationId,
                FromStatus,
                ToStatus,
                ChangedByUserId,
                Reason,
                ChangedAtUtc
            )
            VALUES
            (
                NEWID(),
                @ApplicationId,
                N'SUBMITTED',
                N'IN_PROGRESS',
                @AssignedByUserId,
                N'Application assigned for processing.',
                @AssignedAtUtc
            );
        END;

        COMMIT TRANSACTION;

        SELECT
            aa.ApplicationAssignmentId,
            aa.ApplicationId,
            aa.AssignedToUserId,
            aa.AssignedByUserId,
            aa.AssignedAtUtc,
            aa.UnassignedAtUtc,
            aa.UnassignedByUserId,
            aa.Reason,
            aa.IsActive,
            aa.RowVersion
        FROM [applications].ApplicationAssignments AS aa
        WHERE aa.ApplicationId = @ApplicationId
          AND aa.IsActive = 1;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO


CREATE OR ALTER PROCEDURE
    [applications].Application_GetAssignmentHistory
    @ApplicationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        aa.ApplicationAssignmentId,
        aa.ApplicationId,
        aa.AssignedToUserId,
        aa.AssignedByUserId,
        aa.AssignedAtUtc,
        aa.UnassignedAtUtc,
        aa.UnassignedByUserId,
        aa.Reason,
        aa.IsActive,
        aa.RowVersion
    FROM [applications].ApplicationAssignments AS aa
    WHERE aa.ApplicationId = @ApplicationId
    ORDER BY
        aa.AssignedAtUtc,
        aa.ApplicationAssignmentId;
END;
GO


CREATE OR ALTER PROCEDURE
    [applications].Application_GetAssignedToMe
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ApplicationId,
        a.ApplicationNumber,
        a.ApplicationType,
        a.ApplicantCitizenId,
        a.SubjectCitizenId,
        a.Status,
        a.SubmittedAtUtc,
        a.CreatedAtUtc,
        a.UpdatedAtUtc,
        a.RowVersion
    FROM [applications].Applications AS a
    INNER JOIN [applications].ApplicationAssignments AS aa
        ON aa.ApplicationId = a.ApplicationId
    WHERE aa.AssignedToUserId = @UserId
      AND aa.IsActive = 1
    ORDER BY
        aa.AssignedAtUtc,
        a.ApplicationId;
END;
GO


CREATE OR ALTER PROCEDURE
    [applications].Application_GetUnassigned
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ApplicationId,
        a.ApplicationNumber,
        a.ApplicationType,
        a.ApplicantCitizenId,
        a.SubjectCitizenId,
        a.Status,
        a.SubmittedAtUtc,
        a.CreatedAtUtc,
        a.UpdatedAtUtc,
        a.RowVersion
    FROM [applications].Applications AS a
    WHERE a.Status = N'SUBMITTED'
      AND NOT EXISTS
      (
          SELECT 1
          FROM [applications].ApplicationAssignments AS aa
          WHERE aa.ApplicationId = a.ApplicationId
            AND aa.IsActive = 1
      )
    ORDER BY
        a.SubmittedAtUtc,
        a.ApplicationId;
END;
GO
