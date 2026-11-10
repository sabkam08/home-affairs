namespace HomeAffairs.Contracts.Workflow;

public sealed record WorkflowWorkItemResponse(
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
    string RowVersion);
