using HomeAffairs.Domain.Workflow;

namespace HomeAffairs.Infrastructure.Workflow;

internal sealed class WorkflowInstanceRecord
{
    public Guid WorkflowInstanceId { get; init; }

    public Guid WorkflowDefinitionId { get; init; }

    public Guid ApplicationId { get; init; }

    public string CurrentStateCode { get; init; } =
        string.Empty;

    public DateTime StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public string ApplicationType { get; init; } =
        string.Empty;

    public string WorkflowName { get; init; } =
        string.Empty;

    public int WorkflowVersion { get; init; }

    public WorkflowInstance ToDomain()
    {
        return new WorkflowInstance
        {
            WorkflowInstanceId =
                WorkflowInstanceId,

            WorkflowDefinitionId =
                WorkflowDefinitionId,

            ApplicationId =
                ApplicationId,

            ApplicationType =
                ApplicationType,

            WorkflowName =
                WorkflowName,

            WorkflowVersion =
                WorkflowVersion,

            CurrentStateCode =
                CurrentStateCode,

            StartedAtUtc =
                ToUtc(
                    StartedAtUtc),

            CompletedAtUtc =
                CompletedAtUtc.HasValue
                    ? ToUtc(CompletedAtUtc.Value)
                    : null,

            CreatedAtUtc =
                ToUtc(
                    CreatedAtUtc),

            UpdatedAtUtc =
                UpdatedAtUtc.HasValue
                    ? ToUtc(UpdatedAtUtc.Value)
                    : null,

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
