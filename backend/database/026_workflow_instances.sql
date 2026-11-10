USE [HomeAffairsDb];
GO


/*
    ============================================================
    WorkflowInstances

    Every application is linked to exactly one workflow
    definition/version.

    CurrentStateCode mirrors the application's projected Status.

    The definition is pinned so future workflow-definition
    versions do not silently change applications already
    in progress.
    ============================================================
*/

IF OBJECT_ID(
    N'[workflow].WorkflowInstances',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [workflow].WorkflowInstances
    (
        WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,

        WorkflowDefinitionId UNIQUEIDENTIFIER NOT NULL,

        ApplicationId UNIQUEIDENTIFIER NOT NULL,

        CurrentStateCode NVARCHAR(30) NOT NULL,

        StartedAtUtc DATETIME2(7) NOT NULL,

        CompletedAtUtc DATETIME2(7) NULL,

        CreatedAtUtc DATETIME2(7) NOT NULL,

        UpdatedAtUtc DATETIME2(7) NULL,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_WorkflowInstances
            PRIMARY KEY CLUSTERED (
                WorkflowInstanceId
            ),

        CONSTRAINT FK_WorkflowInstances_Definition
            FOREIGN KEY (
                WorkflowDefinitionId
            )
            REFERENCES [workflow].WorkflowDefinitions (
                WorkflowDefinitionId
            ),

        CONSTRAINT FK_WorkflowInstances_Application
            FOREIGN KEY (
                ApplicationId
            )
            REFERENCES [applications].Applications (
                ApplicationId
            ),

        CONSTRAINT UQ_WorkflowInstances_Application
            UNIQUE (
                ApplicationId
            )
    );

    CREATE NONCLUSTERED INDEX
        IX_WorkflowInstances_CurrentState
    ON [workflow].WorkflowInstances
    (
        CurrentStateCode,
        UpdatedAtUtc
    );

    CREATE NONCLUSTERED INDEX
        IX_WorkflowInstances_Definition
    ON [workflow].WorkflowInstances
    (
        WorkflowDefinitionId
    );
END;
GO


/*
    ============================================================
    Backfill applications created before WorkflowInstances
    existed.

    Each existing application is attached to the currently
    active workflow definition for its ApplicationType.

    Its existing Status becomes CurrentStateCode.

    This is development/schema-evolution compatibility and
    keeps our previously tested applications valid.
    ============================================================
*/

INSERT INTO [workflow].WorkflowInstances
(
    WorkflowInstanceId,
    WorkflowDefinitionId,
    ApplicationId,
    CurrentStateCode,
    StartedAtUtc,
    CompletedAtUtc,
    CreatedAtUtc,
    UpdatedAtUtc
)
SELECT
    NEWID(),

    definition.WorkflowDefinitionId,

    application.ApplicationId,

    application.Status,

    application.CreatedAtUtc,

    CASE
        WHEN application.Status IN
        (
            N'APPROVED',
            N'REJECTED',
            N'COMPLETED',
            N'CANCELLED'
        )
        THEN
            COALESCE(
                application.UpdatedAtUtc,
                application.CreatedAtUtc
            )
        ELSE
            NULL
    END,

    application.CreatedAtUtc,

    application.UpdatedAtUtc

FROM [applications].Applications AS application

INNER JOIN [workflow].WorkflowDefinitions AS definition
    ON definition.ApplicationType =
        application.ApplicationType
   AND definition.IsActive = 1

WHERE NOT EXISTS
(
    SELECT 1
    FROM [workflow].WorkflowInstances AS instance
    WHERE instance.ApplicationId =
        application.ApplicationId
);
GO


/*
    ============================================================
    WorkflowInstance_GetByApplicationId
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkflowInstance_GetByApplicationId
    @ApplicationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        instance.WorkflowInstanceId,
        instance.WorkflowDefinitionId,
        instance.ApplicationId,
        instance.CurrentStateCode,
        instance.StartedAtUtc,
        instance.CompletedAtUtc,
        instance.CreatedAtUtc,
        instance.UpdatedAtUtc,
        instance.RowVersion,

        definition.ApplicationType,
        definition.Name AS WorkflowName,
        definition.Version AS WorkflowVersion

    FROM [workflow].WorkflowInstances AS instance

    INNER JOIN [workflow].WorkflowDefinitions AS definition
        ON definition.WorkflowDefinitionId =
            instance.WorkflowDefinitionId

    WHERE instance.ApplicationId =
        @ApplicationId;
END;
GO


/*
    ============================================================
    WorkflowInstance_GetForApplicant

    Applicant-facing retrieval without exposing another
    Citizen's workflow instance.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkflowInstance_GetForApplicant
    @ApplicationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        instance.WorkflowInstanceId,
        instance.WorkflowDefinitionId,
        instance.ApplicationId,
        instance.CurrentStateCode,
        instance.StartedAtUtc,
        instance.CompletedAtUtc,
        instance.CreatedAtUtc,
        instance.UpdatedAtUtc,
        instance.RowVersion,

        definition.ApplicationType,
        definition.Name AS WorkflowName,
        definition.Version AS WorkflowVersion

    FROM [workflow].WorkflowInstances AS instance

    INNER JOIN [workflow].WorkflowDefinitions AS definition
        ON definition.WorkflowDefinitionId =
            instance.WorkflowDefinitionId

    INNER JOIN [applications].Applications AS application
        ON application.ApplicationId =
            instance.ApplicationId

    INNER JOIN [citizens].Citizens AS applicant
        ON applicant.CitizenId =
            application.ApplicantCitizenId

    WHERE instance.ApplicationId =
            @ApplicationId
      AND applicant.UserId =
            @UserId
      AND applicant.IsActive = 1;
END;
GO


/*
    ============================================================
    WorkflowInstance_GetAvailableTransitions

    Returns legal transitions for the instance's current state.

    Authorization is still evaluated by the API/Application
    permission system.

    This procedure describes what the workflow allows.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkflowInstance_GetAvailableTransitions
    @ApplicationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        transition.WorkflowTransitionId,
        transition.WorkflowDefinitionId,
        transition.FromStateCode,
        transition.ToStateCode,
        transition.ActionCode,
        transition.DisplayName,
        transition.RequiredPermission,
        transition.RequiresReason,
        transition.IsActive

    FROM [workflow].WorkflowInstances AS instance

    INNER JOIN [workflow].WorkflowTransitions AS transition
        ON transition.WorkflowDefinitionId =
            instance.WorkflowDefinitionId
       AND transition.FromStateCode =
            instance.CurrentStateCode

    WHERE instance.ApplicationId =
            @ApplicationId
      AND transition.IsActive = 1

    ORDER BY
        transition.ActionCode;
END;
GO


/*
    ============================================================
    WorkflowInstance_SynchronizeState

    Internal persistence operation used by lifecycle operations.

    It changes workflow state only when:

    1. the requested transition exists in the pinned definition;
    2. the instance is at the expected current state;
    3. RowVersion is current.

    Application lifecycle procedures will begin invoking this
    operation in the following execution-integration slice.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkflowInstance_SynchronizeState
    @ApplicationId UNIQUEIDENTIFIER,
    @ExpectedCurrentStateCode NVARCHAR(30),
    @TargetStateCode NVARCHAR(30),
    @ExpectedRowVersion BINARY(8),
    @ChangedAtUtc DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @WorkflowInstanceId UNIQUEIDENTIFIER;
    DECLARE @WorkflowDefinitionId UNIQUEIDENTIFIER;

    SELECT
        @WorkflowInstanceId =
            instance.WorkflowInstanceId,

        @WorkflowDefinitionId =
            instance.WorkflowDefinitionId

    FROM [workflow].WorkflowInstances AS instance

    WHERE instance.ApplicationId =
        @ApplicationId;

    IF @WorkflowInstanceId IS NULL
    BEGIN;
        THROW 52901,
            'The workflow instance could not be found.',
            1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM [workflow].WorkflowTransitions AS transition
        WHERE transition.WorkflowDefinitionId =
                @WorkflowDefinitionId
          AND transition.FromStateCode =
                @ExpectedCurrentStateCode
          AND transition.ToStateCode =
                @TargetStateCode
          AND transition.IsActive = 1
    )
    BEGIN;
        THROW 52902,
            'The workflow transition is not permitted.',
            1;
    END;

    UPDATE [workflow].WorkflowInstances
    SET
        CurrentStateCode =
            @TargetStateCode,

        CompletedAtUtc =
            CASE
                WHEN @TargetStateCode IN
                (
                    N'APPROVED',
                    N'REJECTED',
                    N'COMPLETED',
                    N'CANCELLED'
                )
                THEN @ChangedAtUtc
                ELSE NULL
            END,

        UpdatedAtUtc =
            @ChangedAtUtc

    WHERE WorkflowInstanceId =
            @WorkflowInstanceId
      AND CurrentStateCode =
            @ExpectedCurrentStateCode
      AND RowVersion =
            @ExpectedRowVersion;

    IF @@ROWCOUNT = 0
    BEGIN;
        THROW 52903,
            'The workflow instance was modified by another request.',
            1;
    END;

    SELECT
        instance.WorkflowInstanceId,
        instance.WorkflowDefinitionId,
        instance.ApplicationId,
        instance.CurrentStateCode,
        instance.StartedAtUtc,
        instance.CompletedAtUtc,
        instance.CreatedAtUtc,
        instance.UpdatedAtUtc,
        instance.RowVersion,

        definition.ApplicationType,
        definition.Name AS WorkflowName,
        definition.Version AS WorkflowVersion

    FROM [workflow].WorkflowInstances AS instance

    INNER JOIN [workflow].WorkflowDefinitions AS definition
        ON definition.WorkflowDefinitionId =
            instance.WorkflowDefinitionId

    WHERE instance.WorkflowInstanceId =
        @WorkflowInstanceId;
END;
GO


/*
    ============================================================
    Alter Application_CreateDraft

    New applications now create their workflow instance in the
    SAME transaction as:

      Application
      Initial DRAFT status history
      WorkflowInstance

    If no active workflow exists, application creation fails
    atomically.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [applications].Application_CreateDraft
    @ApplicationId UNIQUEIDENTIFIER,
    @ApplicationNumber NVARCHAR(30),
    @ApplicationType NVARCHAR(60),
    @ApplicantCitizenId UNIQUEIDENTIFIER,
    @SubjectCitizenId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
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
            WHERE CitizenId =
                @ApplicantCitizenId
              AND UserId =
                @UserId
              AND IsActive = 1
        )
        BEGIN;
            THROW 52401,
                'The applicant citizen could not be found.',
                1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM [citizens].Citizens
            WHERE CitizenId =
                @SubjectCitizenId
              AND IsActive = 1
        )
        BEGIN;
            THROW 52402,
                'The subject citizen could not be found.',
                1;
        END;

        IF @SubjectCitizenId <>
            @ApplicantCitizenId
           AND NOT EXISTS
        (
            SELECT 1
            FROM [citizens].CitizenRelationships
            WHERE RepresentativeCitizenId =
                @ApplicantCitizenId
              AND SubjectCitizenId =
                @SubjectCitizenId
              AND IsActive = 1
        )
        BEGIN;
            THROW 52403,
                'The applicant is not authorised to act for the subject citizen.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM [applications].Applications
            WHERE ApplicationNumber =
                @ApplicationNumber
        )
        BEGIN;
            THROW 52404,
                'The application number already exists.',
                1;
        END;

        DECLARE @WorkflowDefinitionId
            UNIQUEIDENTIFIER;

        DECLARE @InitialStateCode
            NVARCHAR(30);

        SELECT
            @WorkflowDefinitionId =
                definition.WorkflowDefinitionId
        FROM [workflow].WorkflowDefinitions AS definition
        WHERE definition.ApplicationType =
                @ApplicationType
          AND definition.IsActive = 1;

        IF @WorkflowDefinitionId IS NULL
        BEGIN;
            THROW 52904,
                'No active workflow definition exists for this application type.',
                1;
        END;

        SELECT
            @InitialStateCode =
                state.StateCode
        FROM [workflow].WorkflowStates AS state
        WHERE state.WorkflowDefinitionId =
                @WorkflowDefinitionId
          AND state.IsInitial = 1;

        IF @InitialStateCode IS NULL
        BEGIN;
            THROW 52905,
                'The workflow definition does not contain an initial state.',
                1;
        END;

        INSERT INTO [applications].Applications
        (
            ApplicationId,
            ApplicationNumber,
            ApplicationType,
            ApplicantCitizenId,
            SubjectCitizenId,
            Status,
            SubmittedAtUtc,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            @ApplicationId,
            @ApplicationNumber,
            @ApplicationType,
            @ApplicantCitizenId,
            @SubjectCitizenId,
            @InitialStateCode,
            NULL,
            @CreatedAtUtc,
            NULL
        );

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
            NULL,
            @InitialStateCode,
            @UserId,
            N'Application draft created.',
            @CreatedAtUtc
        );

        INSERT INTO [workflow].WorkflowInstances
        (
            WorkflowInstanceId,
            WorkflowDefinitionId,
            ApplicationId,
            CurrentStateCode,
            StartedAtUtc,
            CompletedAtUtc,
            CreatedAtUtc,
            UpdatedAtUtc
        )
        VALUES
        (
            NEWID(),
            @WorkflowDefinitionId,
            @ApplicationId,
            @InitialStateCode,
            @CreatedAtUtc,
            NULL,
            @CreatedAtUtc,
            NULL
        );

        COMMIT TRANSACTION;

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
        WHERE a.ApplicationId =
            @ApplicationId;
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
