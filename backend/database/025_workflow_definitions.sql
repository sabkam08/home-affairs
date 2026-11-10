USE [HomeAffairsDb];
GO

IF SCHEMA_ID(N'workflow') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [workflow]');
END;
GO


/*
    ============================================================
    WorkflowDefinitions

    One workflow definition represents the lifecycle for one
    application type.

    Versions allow us to preserve old definitions after an
    application type's process changes in future.
    ============================================================
*/

IF OBJECT_ID(
    N'[workflow].WorkflowDefinitions',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [workflow].WorkflowDefinitions
    (
        WorkflowDefinitionId UNIQUEIDENTIFIER NOT NULL,

        ApplicationType NVARCHAR(60) NOT NULL,

        Name NVARCHAR(200) NOT NULL,

        Version INT NOT NULL,

        IsActive BIT NOT NULL
            CONSTRAINT DF_WorkflowDefinitions_IsActive
            DEFAULT 1,

        CreatedAtUtc DATETIME2(7) NOT NULL,

        UpdatedAtUtc DATETIME2(7) NULL,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_WorkflowDefinitions
            PRIMARY KEY CLUSTERED (
                WorkflowDefinitionId
            ),

        CONSTRAINT CK_WorkflowDefinitions_Version
            CHECK (Version > 0),

        CONSTRAINT CK_WorkflowDefinitions_Name
            CHECK
            (
                LEN(
                    LTRIM(
                        RTRIM(Name)
                    )
                ) > 0
            )
    );

    CREATE UNIQUE NONCLUSTERED INDEX
        UX_WorkflowDefinitions_TypeVersion
    ON [workflow].WorkflowDefinitions
    (
        ApplicationType,
        Version
    );

    CREATE UNIQUE NONCLUSTERED INDEX
        UX_WorkflowDefinitions_ActiveType
    ON [workflow].WorkflowDefinitions
    (
        ApplicationType
    )
    WHERE IsActive = 1;
END;
GO


/*
    ============================================================
    WorkflowStates
    ============================================================
*/

IF OBJECT_ID(
    N'[workflow].WorkflowStates',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [workflow].WorkflowStates
    (
        WorkflowStateId UNIQUEIDENTIFIER NOT NULL,

        WorkflowDefinitionId UNIQUEIDENTIFIER NOT NULL,

        StateCode NVARCHAR(30) NOT NULL,

        DisplayName NVARCHAR(100) NOT NULL,

        Sequence INT NOT NULL,

        IsInitial BIT NOT NULL,

        IsTerminal BIT NOT NULL,

        CONSTRAINT PK_WorkflowStates
            PRIMARY KEY CLUSTERED (
                WorkflowStateId
            ),

        CONSTRAINT FK_WorkflowStates_Definition
            FOREIGN KEY (
                WorkflowDefinitionId
            )
            REFERENCES [workflow].WorkflowDefinitions (
                WorkflowDefinitionId
            ),

        CONSTRAINT UQ_WorkflowStates_DefinitionCode
            UNIQUE
            (
                WorkflowDefinitionId,
                StateCode
            ),

        CONSTRAINT CK_WorkflowStates_Sequence
            CHECK (Sequence >= 0)
    );

    CREATE UNIQUE NONCLUSTERED INDEX
        UX_WorkflowStates_Initial
    ON [workflow].WorkflowStates
    (
        WorkflowDefinitionId
    )
    WHERE IsInitial = 1;
END;
GO


/*
    ============================================================
    WorkflowTransitions

    RequiredPermission is intentionally stored on the
    transition.

    Examples:
        DRAFT -> SUBMITTED
            APPLICATION.CREATE

        IN_PROGRESS -> AWAITING_APPROVAL
            APPLICATION.PROCESS

        AWAITING_APPROVAL -> APPROVED
            APPLICATION.APPROVE
    ============================================================
*/

IF OBJECT_ID(
    N'[workflow].WorkflowTransitions',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [workflow].WorkflowTransitions
    (
        WorkflowTransitionId UNIQUEIDENTIFIER NOT NULL,

        WorkflowDefinitionId UNIQUEIDENTIFIER NOT NULL,

        FromStateCode NVARCHAR(30) NOT NULL,

        ToStateCode NVARCHAR(30) NOT NULL,

        ActionCode NVARCHAR(60) NOT NULL,

        DisplayName NVARCHAR(150) NOT NULL,

        RequiredPermission NVARCHAR(150) NOT NULL,

        RequiresReason BIT NOT NULL
            CONSTRAINT DF_WorkflowTransitions_RequiresReason
            DEFAULT 0,

        IsActive BIT NOT NULL
            CONSTRAINT DF_WorkflowTransitions_IsActive
            DEFAULT 1,

        CONSTRAINT PK_WorkflowTransitions
            PRIMARY KEY CLUSTERED (
                WorkflowTransitionId
            ),

        CONSTRAINT FK_WorkflowTransitions_Definition
            FOREIGN KEY (
                WorkflowDefinitionId
            )
            REFERENCES [workflow].WorkflowDefinitions (
                WorkflowDefinitionId
            ),

        CONSTRAINT UQ_WorkflowTransitions_Transition
            UNIQUE
            (
                WorkflowDefinitionId,
                FromStateCode,
                ToStateCode,
                ActionCode
            ),

        CONSTRAINT CK_WorkflowTransitions_NotSelf
            CHECK
            (
                FromStateCode <> ToStateCode
            )
    );
END;
GO


/*
    ============================================================
    Seed common workflow definitions

    For the current stage every supported service follows our
    proven core lifecycle.

    Later Birth and Smart ID slices may add service-specific
    states/transitions through new workflow versions rather
    than changing historical definitions.
    ============================================================
*/

DECLARE @NowUtc DATETIME2(7) =
    SYSUTCDATETIME();

DECLARE @ApplicationTypes TABLE
(
    ApplicationType NVARCHAR(60) NOT NULL,
    WorkflowName NVARCHAR(200) NOT NULL
);

INSERT INTO @ApplicationTypes
(
    ApplicationType,
    WorkflowName
)
VALUES
(
    N'BIRTH_REGISTRATION',
    N'Birth Registration Workflow'
),
(
    N'BIRTH_CERTIFICATE_REISSUE',
    N'Birth Certificate Re-Issue Workflow'
),
(
    N'LATE_BIRTH_REGISTRATION',
    N'Late Birth Registration Workflow'
),
(
    N'BIRTH_CERTIFICATE_AMENDMENT',
    N'Birth Certificate Amendment Workflow'
),
(
    N'SMART_ID_NEW',
    N'New Smart ID Workflow'
),
(
    N'SMART_ID_REPLACEMENT_LOST',
    N'Smart ID Lost Replacement Workflow'
),
(
    N'SMART_ID_REPLACEMENT_STOLEN',
    N'Smart ID Stolen Replacement Workflow'
),
(
    N'SMART_ID_REPLACEMENT_DAMAGED',
    N'Smart ID Damaged Replacement Workflow'
),
(
    N'SMART_ID_CORRECTION',
    N'Smart ID Correction Workflow'
);


INSERT INTO [workflow].WorkflowDefinitions
(
    WorkflowDefinitionId,
    ApplicationType,
    Name,
    Version,
    IsActive,
    CreatedAtUtc,
    UpdatedAtUtc
)
SELECT
    NEWID(),
    source.ApplicationType,
    source.WorkflowName,
    1,
    1,
    @NowUtc,
    NULL
FROM @ApplicationTypes AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [workflow].WorkflowDefinitions AS existing
    WHERE existing.ApplicationType =
            source.ApplicationType
      AND existing.Version = 1
);
GO


/*
    ============================================================
    Seed states for every version-1 workflow
    ============================================================
*/

DECLARE @States TABLE
(
    StateCode NVARCHAR(30),
    DisplayName NVARCHAR(100),
    Sequence INT,
    IsInitial BIT,
    IsTerminal BIT
);

INSERT INTO @States
VALUES
(
    N'DRAFT',
    N'Draft',
    10,
    1,
    0
),
(
    N'SUBMITTED',
    N'Submitted',
    20,
    0,
    0
),
(
    N'IN_PROGRESS',
    N'In Progress',
    30,
    0,
    0
),
(
    N'AWAITING_INFORMATION',
    N'Awaiting Information',
    40,
    0,
    0
),
(
    N'AWAITING_APPROVAL',
    N'Awaiting Approval',
    50,
    0,
    0
),
(
    N'APPROVED',
    N'Approved',
    60,
    0,
    1
),
(
    N'REJECTED',
    N'Rejected',
    70,
    0,
    1
),
(
    N'COMPLETED',
    N'Completed',
    80,
    0,
    1
),
(
    N'CANCELLED',
    N'Cancelled',
    90,
    0,
    1
);


INSERT INTO [workflow].WorkflowStates
(
    WorkflowStateId,
    WorkflowDefinitionId,
    StateCode,
    DisplayName,
    Sequence,
    IsInitial,
    IsTerminal
)
SELECT
    NEWID(),
    definition.WorkflowDefinitionId,
    state.StateCode,
    state.DisplayName,
    state.Sequence,
    state.IsInitial,
    state.IsTerminal
FROM [workflow].WorkflowDefinitions AS definition
CROSS JOIN @States AS state
WHERE definition.Version = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM [workflow].WorkflowStates AS existing
      WHERE existing.WorkflowDefinitionId =
                definition.WorkflowDefinitionId
        AND existing.StateCode =
                state.StateCode
  );
GO


/*
    ============================================================
    Seed transitions
    ============================================================
*/

DECLARE @Transitions TABLE
(
    FromStateCode NVARCHAR(30),
    ToStateCode NVARCHAR(30),
    ActionCode NVARCHAR(60),
    DisplayName NVARCHAR(150),
    RequiredPermission NVARCHAR(150),
    RequiresReason BIT
);

INSERT INTO @Transitions
VALUES
(
    N'DRAFT',
    N'SUBMITTED',
    N'SUBMIT',
    N'Submit Application',
    N'APPLICATION.CREATE',
    0
),
(
    N'DRAFT',
    N'CANCELLED',
    N'CANCEL',
    N'Cancel Application',
    N'APPLICATION.CREATE',
    0
),
(
    N'SUBMITTED',
    N'IN_PROGRESS',
    N'ASSIGN',
    N'Assign Application',
    N'APPLICATION.REASSIGN',
    0
),
(
    N'IN_PROGRESS',
    N'AWAITING_INFORMATION',
    N'REQUEST_INFORMATION',
    N'Request Additional Information',
    N'APPLICATION.PROCESS',
    1
),
(
    N'AWAITING_INFORMATION',
    N'IN_PROGRESS',
    N'RESUME_PROCESSING',
    N'Resume Processing',
    N'APPLICATION.PROCESS',
    0
),
(
    N'IN_PROGRESS',
    N'AWAITING_APPROVAL',
    N'SEND_FOR_APPROVAL',
    N'Send for Supervisor Approval',
    N'APPLICATION.PROCESS',
    0
),
(
    N'AWAITING_APPROVAL',
    N'APPROVED',
    N'APPROVE',
    N'Approve Application',
    N'APPLICATION.APPROVE',
    0
),
(
    N'AWAITING_APPROVAL',
    N'REJECTED',
    N'REJECT',
    N'Reject Application',
    N'APPLICATION.APPROVE',
    1
);


INSERT INTO [workflow].WorkflowTransitions
(
    WorkflowTransitionId,
    WorkflowDefinitionId,
    FromStateCode,
    ToStateCode,
    ActionCode,
    DisplayName,
    RequiredPermission,
    RequiresReason,
    IsActive
)
SELECT
    NEWID(),
    definition.WorkflowDefinitionId,
    transition.FromStateCode,
    transition.ToStateCode,
    transition.ActionCode,
    transition.DisplayName,
    transition.RequiredPermission,
    transition.RequiresReason,
    1
FROM [workflow].WorkflowDefinitions AS definition
CROSS JOIN @Transitions AS transition
WHERE definition.Version = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM [workflow].WorkflowTransitions AS existing
      WHERE existing.WorkflowDefinitionId =
                definition.WorkflowDefinitionId
        AND existing.FromStateCode =
                transition.FromStateCode
        AND existing.ToStateCode =
                transition.ToStateCode
        AND existing.ActionCode =
                transition.ActionCode
  );
GO


/*
    ============================================================
    WorkflowDefinition_GetActiveByApplicationType
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkflowDefinition_GetActiveByApplicationType
    @ApplicationType NVARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        definition.WorkflowDefinitionId,
        definition.ApplicationType,
        definition.Name,
        definition.Version,
        definition.IsActive,
        definition.CreatedAtUtc,
        definition.UpdatedAtUtc,
        definition.RowVersion
    FROM [workflow].WorkflowDefinitions AS definition
    WHERE definition.ApplicationType =
            @ApplicationType
      AND definition.IsActive = 1;
END;
GO


/*
    ============================================================
    WorkflowState_GetByDefinition
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkflowState_GetByDefinition
    @WorkflowDefinitionId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        WorkflowStateId,
        WorkflowDefinitionId,
        StateCode,
        DisplayName,
        Sequence,
        IsInitial,
        IsTerminal
    FROM [workflow].WorkflowStates
    WHERE WorkflowDefinitionId =
        @WorkflowDefinitionId
    ORDER BY
        Sequence,
        WorkflowStateId;
END;
GO


/*
    ============================================================
    WorkflowTransition_GetByDefinition
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkflowTransition_GetByDefinition
    @WorkflowDefinitionId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        WorkflowTransitionId,
        WorkflowDefinitionId,
        FromStateCode,
        ToStateCode,
        ActionCode,
        DisplayName,
        RequiredPermission,
        RequiresReason,
        IsActive
    FROM [workflow].WorkflowTransitions
    WHERE WorkflowDefinitionId =
            @WorkflowDefinitionId
      AND IsActive = 1
    ORDER BY
        FromStateCode,
        ToStateCode,
        ActionCode;
END;
GO


/*
    ============================================================
    WorkflowTransition_IsAllowed

    Used by the next workflow-execution slice.

    8A exposes it now so definitions can be tested
    independently before application execution depends on it.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkflowTransition_IsAllowed
    @ApplicationType NVARCHAR(60),
    @FromStateCode NVARCHAR(30),
    @ToStateCode NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(
        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM [workflow].WorkflowDefinitions AS definition
                INNER JOIN [workflow].WorkflowTransitions AS transition
                    ON transition.WorkflowDefinitionId =
                        definition.WorkflowDefinitionId
                WHERE definition.ApplicationType =
                        @ApplicationType
                  AND definition.IsActive = 1
                  AND transition.FromStateCode =
                        @FromStateCode
                  AND transition.ToStateCode =
                        @ToStateCode
                  AND transition.IsActive = 1
            )
            THEN 1
            ELSE 0
        END
        AS BIT
    ) AS IsAllowed;
END;
GO
