namespace HomeAffairs.Infrastructure.Workflow;

internal sealed class WorkflowDefinitionRecord
{
    public Guid WorkflowDefinitionId { get; init; }

    public string ApplicationType { get; init; } =
        string.Empty;

    public string Name { get; init; } =
        string.Empty;

    public int Version { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
