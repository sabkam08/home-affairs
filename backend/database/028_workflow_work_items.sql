USE [HomeAffairsDb];
GO

/*
    ============================================================
    Workflow Work Items

    Work items represent human work required by a workflow.

    Workflow state answers:
        "Where is the application?"

    Work item answers:
        "Who currently needs to do something?"
    ============================================================
*/

IF OBJECT_ID(
    N'[workflow].WorkItems',
    N'U'
) IS NULL
BEGIN
    CREATE TABLE [workflow].WorkItems
    (
        WorkItemId UNIQUEIDENTIFIER NOT NULL,

        WorkflowInstanceId UNIQUEIDENTIFIER NOT NULL,

        ApplicationId UNIQUEIDENTIFIER NOT NULL,

        WorkItemType NVARCHAR(60) NOT NULL,

        Status NVARCHAR(30) NOT NULL,

        AssignedToUserId UNIQUEIDENTIFIER NULL,

        AssignedRoleCode NVARCHAR(60) NULL,

        CreatedAtUtc DATETIME2(7) NOT NULL,

        ClaimedAtUtc DATETIME2(7) NULL,

        CompletedAtUtc DATETIME2(7) NULL,

        CancelledAtUtc DATETIME2(7) NULL,

        CompletedByUserId UNIQUEIDENTIFIER NULL,

        Reason NVARCHAR(500) NULL,

        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_WorkItems
            PRIMARY KEY CLUSTERED (
                WorkItemId
            ),

        CONSTRAINT FK_WorkItems_WorkflowInstance
            FOREIGN KEY (
                WorkflowInstanceId
            )
            REFERENCES [workflow].WorkflowInstances (
                WorkflowInstanceId
            ),

        CONSTRAINT FK_WorkItems_Application
            FOREIGN KEY (
                ApplicationId
            )
            REFERENCES [applications].Applications (
                ApplicationId
            ),

        CONSTRAINT FK_WorkItems_AssignedTo
            FOREIGN KEY (
                AssignedToUserId
            )
            REFERENCES [identity].Users (
                UserId
            ),

        CONSTRAINT FK_WorkItems_CompletedBy
            FOREIGN KEY (
                CompletedByUserId
            )
            REFERENCES [identity].Users (
                UserId
            ),

        CONSTRAINT CK_WorkItems_Type
            CHECK
            (
                WorkItemType IN
                (
                    N'PRACTITIONER_REVIEW',
                    N'SUPERVISOR_APPROVAL'
                )
            ),

        CONSTRAINT CK_WorkItems_Status
            CHECK
            (
                Status IN
                (
                    N'OPEN',
                    N'CLAIMED',
                    N'COMPLETED',
                    N'CANCELLED'
                )
            )
    );

    /*
        An application may have only one active work item
        at a time.
    */
    CREATE UNIQUE NONCLUSTERED INDEX
        UX_WorkItems_ActiveApplication
    ON [workflow].WorkItems
    (
        ApplicationId
    )
    WHERE Status IN
    (
        N'OPEN',
        N'CLAIMED'
    );

    CREATE NONCLUSTERED INDEX
        IX_WorkItems_AssignedUser
    ON [workflow].WorkItems
    (
        AssignedToUserId,
        Status,
        CreatedAtUtc
    );

    CREATE NONCLUSTERED INDEX
        IX_WorkItems_AssignedRole
    ON [workflow].WorkItems
    (
        AssignedRoleCode,
        Status,
        CreatedAtUtc
    );

    CREATE NONCLUSTERED INDEX
        IX_WorkItems_WorkflowInstance
    ON [workflow].WorkItems
    (
        WorkflowInstanceId,
        CreatedAtUtc
    );
END;
GO


/*
    ============================================================
    WorkItem_CreatePractitioner

    Called when an application is initially assigned or when
    processing resumes after waiting for information.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkItem_CreatePractitioner
    @ApplicationId UNIQUEIDENTIFIER,
    @AssignedToUserId UNIQUEIDENTIFIER,
    @CreatedAtUtc DATETIME2(7),
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @WorkflowInstanceId UNIQUEIDENTIFIER;

    SELECT
        @WorkflowInstanceId =
            WorkflowInstanceId
    FROM [workflow].WorkflowInstances
    WHERE ApplicationId =
        @ApplicationId;

    IF @WorkflowInstanceId IS NULL
    BEGIN;
        THROW 53001,
            'The workflow instance could not be found.',
            1;
    END;

    /*
        Close any currently active work item defensively.
    */
    UPDATE [workflow].WorkItems
    SET
        Status =
            N'CANCELLED',

        CancelledAtUtc =
            @CreatedAtUtc,

        Reason =
            COALESCE(
                @Reason,
                Reason
            )

    WHERE ApplicationId =
            @ApplicationId
      AND Status IN
      (
          N'OPEN',
          N'CLAIMED'
      );

    INSERT INTO [workflow].WorkItems
    (
        WorkItemId,
        WorkflowInstanceId,
        ApplicationId,
        WorkItemType,
        Status,
        AssignedToUserId,
        AssignedRoleCode,
        CreatedAtUtc,
        ClaimedAtUtc,
        CompletedAtUtc,
        CancelledAtUtc,
        CompletedByUserId,
        Reason
    )
    VALUES
    (
        NEWID(),
        @WorkflowInstanceId,
        @ApplicationId,
        N'PRACTITIONER_REVIEW',
        N'CLAIMED',
        @AssignedToUserId,
        N'PRACTITIONER',
        @CreatedAtUtc,
        @CreatedAtUtc,
        NULL,
        NULL,
        NULL,
        @Reason
    );
END;
GO


/*
    ============================================================
    WorkItem_CreateSupervisorApproval
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkItem_CreateSupervisorApproval
    @ApplicationId UNIQUEIDENTIFIER,
    @CreatedAtUtc DATETIME2(7),
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @WorkflowInstanceId UNIQUEIDENTIFIER;

    SELECT
        @WorkflowInstanceId =
            WorkflowInstanceId
    FROM [workflow].WorkflowInstances
    WHERE ApplicationId =
        @ApplicationId;

    IF @WorkflowInstanceId IS NULL
    BEGIN;
        THROW 53001,
            'The workflow instance could not be found.',
            1;
    END;

    /*
        Practitioner work finishes when the application
        is sent for approval.
    */
    UPDATE [workflow].WorkItems
    SET
        Status =
            N'COMPLETED',

        CompletedAtUtc =
            @CreatedAtUtc

    WHERE ApplicationId =
            @ApplicationId
      AND WorkItemType =
            N'PRACTITIONER_REVIEW'
      AND Status IN
      (
          N'OPEN',
          N'CLAIMED'
      );

    INSERT INTO [workflow].WorkItems
    (
        WorkItemId,
        WorkflowInstanceId,
        ApplicationId,
        WorkItemType,
        Status,
        AssignedToUserId,
        AssignedRoleCode,
        CreatedAtUtc,
        ClaimedAtUtc,
        CompletedAtUtc,
        CancelledAtUtc,
        CompletedByUserId,
        Reason
    )
    VALUES
    (
        NEWID(),
        @WorkflowInstanceId,
        @ApplicationId,
        N'SUPERVISOR_APPROVAL',
        N'OPEN',
        NULL,
        N'SUPERVISOR',
        @CreatedAtUtc,
        NULL,
        NULL,
        NULL,
        NULL,
        @Reason
    );
END;
GO


/*
    ============================================================
    WorkItem_CompleteActive
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkItem_CompleteActive
    @ApplicationId UNIQUEIDENTIFIER,
    @CompletedByUserId UNIQUEIDENTIFIER,
    @CompletedAtUtc DATETIME2(7),
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [workflow].WorkItems
    SET
        Status =
            N'COMPLETED',

        CompletedAtUtc =
            @CompletedAtUtc,

        CompletedByUserId =
            @CompletedByUserId,

        Reason =
            COALESCE(
                NULLIF(
                    LTRIM(RTRIM(@Reason)),
                    N''
                ),
                Reason
            )

    WHERE ApplicationId =
            @ApplicationId
      AND Status IN
      (
          N'OPEN',
          N'CLAIMED'
      );
END;
GO


/*
    ============================================================
    WorkItem_CancelActive

    Used when processing pauses for additional information.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkItem_CancelActive
    @ApplicationId UNIQUEIDENTIFIER,
    @CancelledAtUtc DATETIME2(7),
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [workflow].WorkItems
    SET
        Status =
            N'CANCELLED',

        CancelledAtUtc =
            @CancelledAtUtc,

        Reason =
            COALESCE(
                NULLIF(
                    LTRIM(RTRIM(@Reason)),
                    N''
                ),
                Reason
            )

    WHERE ApplicationId =
            @ApplicationId
      AND Status IN
      (
          N'OPEN',
          N'CLAIMED'
      );
END;
GO


/*
    ============================================================
    WorkItem_ReassignPractitioner

    Reassignment keeps the same logical practitioner work item
    but changes ownership.

    Previous application assignment history remains in the
    ApplicationAssignments table.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkItem_ReassignPractitioner
    @ApplicationId UNIQUEIDENTIFIER,
    @AssignedToUserId UNIQUEIDENTIFIER,
    @ChangedAtUtc DATETIME2(7),
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE [workflow].WorkItems
    SET
        AssignedToUserId =
            @AssignedToUserId,

        AssignedRoleCode =
            N'PRACTITIONER',

        Status =
            N'CLAIMED',

        ClaimedAtUtc =
            @ChangedAtUtc,

        Reason =
            COALESCE(
                NULLIF(
                    LTRIM(RTRIM(@Reason)),
                    N''
                ),
                Reason
            )

    WHERE ApplicationId =
            @ApplicationId
      AND WorkItemType =
            N'PRACTITIONER_REVIEW'
      AND Status IN
      (
          N'OPEN',
          N'CLAIMED'
      );

    IF @@ROWCOUNT = 0
    BEGIN
        EXEC [workflow].WorkItem_CreatePractitioner
            @ApplicationId =
                @ApplicationId,
            @AssignedToUserId =
                @AssignedToUserId,
            @CreatedAtUtc =
                @ChangedAtUtc,
            @Reason =
                @Reason;
    END;
END;
GO


/*
    ============================================================
    WorkItem_GetAssignedToUser
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkItem_GetAssignedToUser
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        wi.WorkItemId,
        wi.WorkflowInstanceId,
        wi.ApplicationId,
        wi.WorkItemType,
        wi.Status,
        wi.AssignedToUserId,
        wi.AssignedRoleCode,
        wi.CreatedAtUtc,
        wi.ClaimedAtUtc,
        wi.CompletedAtUtc,
        wi.CancelledAtUtc,
        wi.CompletedByUserId,
        wi.Reason,
        wi.RowVersion
    FROM [workflow].WorkItems AS wi
    WHERE wi.AssignedToUserId =
            @UserId
      AND wi.Status IN
      (
          N'OPEN',
          N'CLAIMED'
      )
    ORDER BY
        wi.CreatedAtUtc,
        wi.WorkItemId;
END;
GO


/*
    ============================================================
    WorkItem_GetOpenByRole

    Primarily used for supervisor approval queues.
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkItem_GetOpenByRole
    @RoleCode NVARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        wi.WorkItemId,
        wi.WorkflowInstanceId,
        wi.ApplicationId,
        wi.WorkItemType,
        wi.Status,
        wi.AssignedToUserId,
        wi.AssignedRoleCode,
        wi.CreatedAtUtc,
        wi.ClaimedAtUtc,
        wi.CompletedAtUtc,
        wi.CancelledAtUtc,
        wi.CompletedByUserId,
        wi.Reason,
        wi.RowVersion
    FROM [workflow].WorkItems AS wi
    WHERE wi.AssignedRoleCode =
            @RoleCode
      AND wi.Status =
            N'OPEN'
    ORDER BY
        wi.CreatedAtUtc,
        wi.WorkItemId;
END;
GO


/*
    ============================================================
    WorkItem_GetHistoryByApplication
    ============================================================
*/

CREATE OR ALTER PROCEDURE
    [workflow].WorkItem_GetHistoryByApplication
    @ApplicationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        wi.WorkItemId,
        wi.WorkflowInstanceId,
        wi.ApplicationId,
        wi.WorkItemType,
        wi.Status,
        wi.AssignedToUserId,
        wi.AssignedRoleCode,
        wi.CreatedAtUtc,
        wi.ClaimedAtUtc,
        wi.CompletedAtUtc,
        wi.CancelledAtUtc,
        wi.CompletedByUserId,
        wi.Reason,
        wi.RowVersion
    FROM [workflow].WorkItems AS wi
    WHERE wi.ApplicationId =
            @ApplicationId
    ORDER BY
        wi.CreatedAtUtc,
        wi.WorkItemId;
END;
GO


/*
    ============================================================
    Backfill currently actionable applications.

    - IN_PROGRESS gets Practitioner Review using active assignee.
    - AWAITING_APPROVAL gets Supervisor Approval.
    - We deliberately do not create active work while in
      AWAITING_INFORMATION.
    ============================================================
*/

INSERT INTO [workflow].WorkItems
(
    WorkItemId,
    WorkflowInstanceId,
    ApplicationId,
    WorkItemType,
    Status,
    AssignedToUserId,
    AssignedRoleCode,
    CreatedAtUtc,
    ClaimedAtUtc,
    CompletedAtUtc,
    CancelledAtUtc,
    CompletedByUserId,
    Reason
)
SELECT
    NEWID(),
    instance.WorkflowInstanceId,
    application.ApplicationId,
    N'PRACTITIONER_REVIEW',
    N'CLAIMED',
    assignment.AssignedToUserId,
    N'PRACTITIONER',
    COALESCE(
        assignment.AssignedAtUtc,
        application.UpdatedAtUtc,
        application.CreatedAtUtc
    ),
    assignment.AssignedAtUtc,
    NULL,
    NULL,
    NULL,
    N'Backfilled active practitioner work item.'
FROM [applications].Applications AS application
INNER JOIN [workflow].WorkflowInstances AS instance
    ON instance.ApplicationId =
        application.ApplicationId
INNER JOIN [applications].ApplicationAssignments AS assignment
    ON assignment.ApplicationId =
        application.ApplicationId
   AND assignment.IsActive = 1
WHERE application.Status =
        N'IN_PROGRESS'
  AND NOT EXISTS
  (
      SELECT 1
      FROM [workflow].WorkItems AS existing
      WHERE existing.ApplicationId =
            application.ApplicationId
        AND existing.Status IN
        (
            N'OPEN',
            N'CLAIMED'
        )
  );
GO


INSERT INTO [workflow].WorkItems
(
    WorkItemId,
    WorkflowInstanceId,
    ApplicationId,
    WorkItemType,
    Status,
    AssignedToUserId,
    AssignedRoleCode,
    CreatedAtUtc,
    ClaimedAtUtc,
    CompletedAtUtc,
    CancelledAtUtc,
    CompletedByUserId,
    Reason
)
SELECT
    NEWID(),
    instance.WorkflowInstanceId,
    application.ApplicationId,
    N'SUPERVISOR_APPROVAL',
    N'OPEN',
    NULL,
    N'SUPERVISOR',
    COALESCE(
        application.UpdatedAtUtc,
        application.CreatedAtUtc
    ),
    NULL,
    NULL,
    NULL,
    NULL,
    N'Backfilled supervisor approval work item.'
FROM [applications].Applications AS application
INNER JOIN [workflow].WorkflowInstances AS instance
    ON instance.ApplicationId =
        application.ApplicationId
WHERE application.Status =
        N'AWAITING_APPROVAL'
  AND NOT EXISTS
  (
      SELECT 1
      FROM [workflow].WorkItems AS existing
      WHERE existing.ApplicationId =
            application.ApplicationId
        AND existing.Status IN
        (
            N'OPEN',
            N'CLAIMED'
        )
  );
GO
