using MediatR;

namespace HomeAffairs.Application.Workflow.WorkItems;

public sealed record GetApplicationWorkItemHistoryQuery(
    Guid ApplicationId)
    : IRequest<
        IReadOnlyCollection<
            WorkflowWorkItemResult>>;
