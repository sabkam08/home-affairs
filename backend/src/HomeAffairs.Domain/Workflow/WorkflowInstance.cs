namespace HomeAffairs.Domain.Workflow;

public sealed class WorkflowInstance
{
    public Guid WorkflowInstanceId { get; init; }

    public Guid WorkflowDefinitionId { get; init; }

    public Guid ApplicationId { get; init; }

    public string ApplicationType { get; init; } =
        string.Empty;

    public string WorkflowName { get; init; } =
        string.Empty;

    public int WorkflowVersion { get; init; }

    public string CurrentStateCode { get; init; } =
        string.Empty;

    public DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public bool IsCompleted =>
        CompletedAtUtc.HasValue;
}
