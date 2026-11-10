namespace HomeAffairs.Contracts.Workflow;

public sealed record WorkflowStateResponse(
    string StateCode,
    string DisplayName,
    int Sequence,
    bool IsInitial,
    bool IsTerminal);
