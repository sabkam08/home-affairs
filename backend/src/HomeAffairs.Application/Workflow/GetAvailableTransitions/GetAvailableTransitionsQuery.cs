using MediatR;

namespace HomeAffairs.Application.Workflow.GetAvailableTransitions;

public sealed record GetAvailableTransitionsQuery(
    Guid ApplicationId)
    : IRequest<
        IReadOnlyCollection<
            AvailableWorkflowTransitionResult>>;

public sealed record AvailableWorkflowTransitionResult(
    string FromStateCode,
    string ToStateCode,
    string ActionCode,
    string DisplayName,
    string RequiredPermission,
    bool RequiresReason);
