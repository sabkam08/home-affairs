namespace HomeAffairs.Contracts.Workflow;

public sealed record WorkflowTransitionResponse(
    string FromStateCode,
    string ToStateCode,
    string ActionCode,
    string DisplayName,
    string RequiredPermission,
    bool RequiresReason);
