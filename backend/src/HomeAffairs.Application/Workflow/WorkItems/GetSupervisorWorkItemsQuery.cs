using MediatR;

namespace HomeAffairs.Application.Workflow.WorkItems;

public sealed record GetSupervisorWorkItemsQuery
    : IRequest<
        IReadOnlyCollection<
            WorkflowWorkItemResult>>;
