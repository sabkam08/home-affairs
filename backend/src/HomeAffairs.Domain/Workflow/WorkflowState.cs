namespace HomeAffairs.Domain.Workflow;

public sealed class WorkflowState
{
    public Guid WorkflowStateId { get; init; }

    public Guid WorkflowDefinitionId { get; init; }

    public string StateCode { get; init; } =
        string.Empty;

    public string DisplayName { get; init; } =
        string.Empty;

    public int Sequence { get; init; }

    public bool IsInitial { get; init; }

    public bool IsTerminal { get; init; }
}
