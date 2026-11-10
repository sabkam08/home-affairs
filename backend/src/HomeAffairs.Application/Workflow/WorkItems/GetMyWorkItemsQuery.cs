using MediatR;

namespace HomeAffairs.Application.Workflow.WorkItems;

public sealed record GetMyWorkItemsQuery
    : IRequest<
        IReadOnlyCollection<
            WorkflowWorkItemResult>>;
