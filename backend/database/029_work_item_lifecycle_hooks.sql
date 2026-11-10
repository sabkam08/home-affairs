USE [HomeAffairsDb];
GO

/*
    ============================================================
    IMPORTANT

    This migration updates the work-item layer by wrapping the
    already tested lifecycle procedures through lightweight
    database triggers.

    The triggers execute in the SAME SQL transaction as the
    application change. If work-item maintenance fails, the
    originating transaction also fails.

    This avoids duplicating the large, already-tested lifecycle
    procedures a second time.
    ============================================================
*/


/*
    ============================================================
    Application state -> Work Item synchronization

    We intentionally derive work-item creation/completion from
    authoritative Application status changes.

    Application/Workflow state synchronization is already
    enforced by 027.
    ============================================================
*/

CREATE OR ALTER TRIGGER
    [applications].TR_Applications_WorkItems
ON [applications].Applications
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    /*
        --------------------------------------------------------
        SUBMITTED -> IN_PROGRESS

        Create practitioner review using current assignment.
        --------------------------------------------------------
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
        inserted.ApplicationId,
        N'PRACTITIONER_REVIEW',
        N'CLAIMED',
        assignment.AssignedToUserId,
        N'PRACTITIONER',
        inserted.UpdatedAtUtc,
        inserted.UpdatedAtUtc,
        NULL,
        NULL,
        NULL,
        N'Application entered practitioner processing.'

    FROM inserted

    INNER JOIN deleted
        ON deleted.ApplicationId =
            inserted.ApplicationId

    INNER JOIN [workflow].WorkflowInstances AS instance
        ON instance.ApplicationId =
            inserted.ApplicationId

    INNER JOIN [applications].ApplicationAssignments AS assignment
        ON assignment.ApplicationId =
            inserted.ApplicationId
       AND assignment.IsActive = 1

    WHERE deleted.Status =
            N'SUBMITTED'
      AND inserted.Status =
            N'IN_PROGRESS'
      AND NOT EXISTS
      (
          SELECT 1
          FROM [workflow].WorkItems AS existing
          WHERE existing.ApplicationId =
                inserted.ApplicationId
            AND existing.Status IN
            (
                N'OPEN',
                N'CLAIMED'
            )
      );


    /*
        --------------------------------------------------------
        IN_PROGRESS -> AWAITING_INFORMATION

        No staff work is active while waiting for applicant
        information.
        --------------------------------------------------------
    */

    UPDATE workItem
    SET
        Status =
            N'CANCELLED',

        CancelledAtUtc =
            inserted.UpdatedAtUtc,

        Reason =
            COALESCE(
                workItem.Reason,
                N'Awaiting additional information.'
            )

    FROM [workflow].WorkItems AS workItem

    INNER JOIN inserted
        ON inserted.ApplicationId =
            workItem.ApplicationId

    INNER JOIN deleted
        ON deleted.ApplicationId =
            inserted.ApplicationId

    WHERE deleted.Status =
            N'IN_PROGRESS'
      AND inserted.Status =
            N'AWAITING_INFORMATION'
      AND workItem.Status IN
      (
          N'OPEN',
          N'CLAIMED'
      );


    /*
        --------------------------------------------------------
        AWAITING_INFORMATION -> IN_PROGRESS

        Create a fresh practitioner work item using current
        active assignment.
        --------------------------------------------------------
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
        inserted.ApplicationId,
        N'PRACTITIONER_REVIEW',
        N'CLAIMED',
        assignment.AssignedToUserId,
        N'PRACTITIONER',
        inserted.UpdatedAtUtc,
        inserted.UpdatedAtUtc,
        NULL,
        NULL,
        NULL,
        N'Processing resumed after information received.'

    FROM inserted

    INNER JOIN deleted
        ON deleted.ApplicationId =
            inserted.ApplicationId

    INNER JOIN [workflow].WorkflowInstances AS instance
        ON instance.ApplicationId =
            inserted.ApplicationId

    INNER JOIN [applications].ApplicationAssignments AS assignment
        ON assignment.ApplicationId =
            inserted.ApplicationId
       AND assignment.IsActive = 1

    WHERE deleted.Status =
            N'AWAITING_INFORMATION'
      AND inserted.Status =
            N'IN_PROGRESS'
      AND NOT EXISTS
      (
          SELECT 1
          FROM [workflow].WorkItems AS existing
          WHERE existing.ApplicationId =
                inserted.ApplicationId
            AND existing.Status IN
            (
                N'OPEN',
                N'CLAIMED'
            )
      );


    /*
        --------------------------------------------------------
        IN_PROGRESS -> AWAITING_APPROVAL

        Complete practitioner work.
        --------------------------------------------------------
    */

    UPDATE workItem
    SET
        Status =
            N'COMPLETED',

        CompletedAtUtc =
            inserted.UpdatedAtUtc

    FROM [workflow].WorkItems AS workItem

    INNER JOIN inserted
        ON inserted.ApplicationId =
            workItem.ApplicationId

    INNER JOIN deleted
        ON deleted.ApplicationId =
            inserted.ApplicationId

    WHERE deleted.Status =
            N'IN_PROGRESS'
      AND inserted.Status =
            N'AWAITING_APPROVAL'
      AND workItem.WorkItemType =
            N'PRACTITIONER_REVIEW'
      AND workItem.Status IN
      (
          N'OPEN',
          N'CLAIMED'
      );


    /*
        Create supervisor approval work.
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
        inserted.ApplicationId,
        N'SUPERVISOR_APPROVAL',
        N'OPEN',
        NULL,
        N'SUPERVISOR',
        inserted.UpdatedAtUtc,
        NULL,
        NULL,
        NULL,
        NULL,
        N'Application requires supervisor decision.'

    FROM inserted

    INNER JOIN deleted
        ON deleted.ApplicationId =
            inserted.ApplicationId

    INNER JOIN [workflow].WorkflowInstances AS instance
        ON instance.ApplicationId =
            inserted.ApplicationId

    WHERE deleted.Status =
            N'IN_PROGRESS'
      AND inserted.Status =
            N'AWAITING_APPROVAL'
      AND NOT EXISTS
      (
          SELECT 1
          FROM [workflow].WorkItems AS existing
          WHERE existing.ApplicationId =
                inserted.ApplicationId
            AND existing.Status IN
            (
                N'OPEN',
                N'CLAIMED'
            )
      );


    /*
        --------------------------------------------------------
        AWAITING_APPROVAL -> APPROVED / REJECTED

        Close supervisor work.
        --------------------------------------------------------
    */

    UPDATE workItem
    SET
        Status =
            N'COMPLETED',

        CompletedAtUtc =
            inserted.UpdatedAtUtc,

        CompletedByUserId =
            history.ChangedByUserId,

        Reason =
            COALESCE(
                history.Reason,
                workItem.Reason
            )

    FROM [workflow].WorkItems AS workItem

    INNER JOIN inserted
        ON inserted.ApplicationId =
            workItem.ApplicationId

    INNER JOIN deleted
        ON deleted.ApplicationId =
            inserted.ApplicationId

    OUTER APPLY
    (
        SELECT TOP (1)
            statusHistory.ChangedByUserId,
            statusHistory.Reason
        FROM [applications].ApplicationStatusHistory
            AS statusHistory
        WHERE statusHistory.ApplicationId =
                inserted.ApplicationId
          AND statusHistory.ToStatus =
                inserted.Status
        ORDER BY
            statusHistory.ChangedAtUtc DESC,
            statusHistory.ApplicationStatusHistoryId DESC
    ) AS history

    WHERE deleted.Status =
            N'AWAITING_APPROVAL'
      AND inserted.Status IN
      (
          N'APPROVED',
          N'REJECTED'
      )
      AND workItem.WorkItemType =
            N'SUPERVISOR_APPROVAL'
      AND workItem.Status IN
      (
          N'OPEN',
          N'CLAIMED'
      );
END;
GO


/*
    ============================================================
    Assignment reassignment -> Work Item ownership synchronization

    Reassignment does not change application status, so it
    requires an assignment trigger rather than the application
    status trigger above.
    ============================================================
*/

CREATE OR ALTER TRIGGER
    [applications].TR_ApplicationAssignments_WorkItems
ON [applications].ApplicationAssignments
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE workItem
    SET
        AssignedToUserId =
            inserted.AssignedToUserId,

        AssignedRoleCode =
            N'PRACTITIONER',

        Status =
            N'CLAIMED',

        ClaimedAtUtc =
            inserted.AssignedAtUtc,

        Reason =
            COALESCE(
                inserted.Reason,
                workItem.Reason
            )

    FROM [workflow].WorkItems AS workItem

    INNER JOIN inserted
        ON inserted.ApplicationId =
            workItem.ApplicationId

    WHERE inserted.IsActive = 1
      AND workItem.WorkItemType =
            N'PRACTITIONER_REVIEW'
      AND workItem.Status IN
      (
          N'OPEN',
          N'CLAIMED'
      );
END;
GO
