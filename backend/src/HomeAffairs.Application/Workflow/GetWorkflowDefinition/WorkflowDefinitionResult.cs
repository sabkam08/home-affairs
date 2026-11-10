namespace HomeAffairs.Application.Workflow.GetWorkflowDefinition;

public sealed record WorkflowDefinitionResult(
    Guid WorkflowDefinitionId,
    string ApplicationType,
    string Name,
    int Version,
    bool IsActive,
    IReadOnlyCollection<WorkflowStateResult> States,
    IReadOnlyCollection<WorkflowTransitionResult>
        Transitions);

public sealed record WorkflowStateResult(
    string StateCode,
    string DisplayName,
    int Sequence,
    bool IsInitial,
    bool IsTerminal);

public sealed record WorkflowTransitionResult(
    string FromStateCode,
    string ToStateCode,
    string ActionCode,
    string DisplayName,
    string RequiredPermission,
    bool RequiresReason);
