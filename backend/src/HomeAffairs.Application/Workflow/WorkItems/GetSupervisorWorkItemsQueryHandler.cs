using HomeAffairs.Application.Abstractions.Workflow;
using MediatR;

namespace HomeAffairs.Application.Workflow.WorkItems;

internal sealed class GetSupervisorWorkItemsQueryHandler(
    IWorkflowWorkItemRepository repository)
    : IRequestHandler<
        GetSupervisorWorkItemsQuery,
        IReadOnlyCollection<
            WorkflowWorkItemResult>>
{
    public async Task<
        IReadOnlyCollection<
            WorkflowWorkItemResult>>
        Handle(
            GetSupervisorWorkItemsQuery request,
            CancellationToken cancellationToken)
    {
        var items =
            await repository.GetOpenByRoleAsync(
                "SUPERVISOR",
                cancellationToken);

        return items
            .Select(
                GetMyWorkItemsQueryHandler.Map)
            .ToArray();
    }
}
