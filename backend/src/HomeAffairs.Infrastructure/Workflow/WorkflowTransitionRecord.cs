namespace HomeAffairs.Infrastructure.Workflow;

internal sealed class WorkflowTransitionRecord
{
    public Guid WorkflowTransitionId { get; init; }

    public Guid WorkflowDefinitionId { get; init; }

    public string FromStateCode { get; init; } =
        string.Empty;

    public string ToStateCode { get; init; } =
        string.Empty;

    public string ActionCode { get; init; } =
        string.Empty;

    public string DisplayName { get; init; } =
        string.Empty;

    public string RequiredPermission { get; init; } =
        string.Empty;

    public bool RequiresReason { get; init; }

    public bool IsActive { get; init; }
}
