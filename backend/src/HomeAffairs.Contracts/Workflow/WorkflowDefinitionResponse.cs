namespace HomeAffairs.Contracts.Workflow;

public sealed record WorkflowDefinitionResponse(
    Guid WorkflowDefinitionId,
    string ApplicationType,
    string Name,
    int Version,
    bool IsActive,
    IReadOnlyCollection<WorkflowStateResponse> States,
    IReadOnlyCollection<WorkflowTransitionResponse>
        Transitions);
