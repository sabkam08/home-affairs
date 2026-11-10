namespace HomeAffairs.Application.Workflow.WorkItems;

public sealed record WorkflowWorkItemResult(
    Guid WorkItemId,
    Guid WorkflowInstanceId,
    Guid ApplicationId,
    string WorkItemType,
    string Status,
    Guid? AssignedToUserId,
    string? AssignedRoleCode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClaimedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    Guid? CompletedByUserId,
    string? Reason,
    byte[] RowVersion);
