using HomeAffairs.Domain.Workflow;

namespace HomeAffairs.Infrastructure.Workflow;

internal sealed class WorkflowWorkItemRecord
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

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ClaimedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public DateTime? CancelledAtUtc { get; init; }

    public Guid? CompletedByUserId { get; init; }

    public string? Reason { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public WorkflowWorkItem ToDomain()
    {
        return new WorkflowWorkItem
        {
            WorkItemId =
                WorkItemId,

            WorkflowInstanceId =
                WorkflowInstanceId,

            ApplicationId =
                ApplicationId,

            WorkItemType =
                WorkItemType,

            Status =
                Status,

            AssignedToUserId =
                AssignedToUserId,

            AssignedRoleCode =
                AssignedRoleCode,

            CreatedAtUtc =
                ToUtc(CreatedAtUtc),

            ClaimedAtUtc =
                ClaimedAtUtc.HasValue
                    ? ToUtc(ClaimedAtUtc.Value)
                    : null,

            CompletedAtUtc =
                CompletedAtUtc.HasValue
                    ? ToUtc(CompletedAtUtc.Value)
                    : null,

            CancelledAtUtc =
                CancelledAtUtc.HasValue
                    ? ToUtc(CancelledAtUtc.Value)
                    : null,

            CompletedByUserId =
                CompletedByUserId,

            Reason =
                Reason,

            RowVersion =
                RowVersion
        };
    }

    private static DateTimeOffset ToUtc(
        DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc));
    }
}
