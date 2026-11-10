namespace HomeAffairs.Domain.Workflow;

public sealed class WorkflowWorkItem
{
    public Guid WorkItemId { get; init; }

    public Guid WorkflowInstanceId { get; init; }

    public Guid ApplicationId { get; init; }

    public string WorkItemType { get; init; } =
        string.Empty;

    public string Status { get; init; } =
        string.Empty;

    public Guid? AssignedToUserId { get; init; }

    public string? AssignedRoleCode { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ClaimedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; init; }

    public DateTimeOffset? CancelledAtUtc { get; init; }

    public Guid? CompletedByUserId { get; init; }

    public string? Reason { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public bool IsActive =>
        string.Equals(
            Status,
            WorkItemStatuses.Open,
            StringComparison.Ordinal)
        ||
        string.Equals(
            Status,
            WorkItemStatuses.Claimed,
            StringComparison.Ordinal);
}
