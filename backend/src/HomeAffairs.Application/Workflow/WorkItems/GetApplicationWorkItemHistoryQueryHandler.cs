using HomeAffairs.Application.Abstractions.Workflow;
using MediatR;

namespace HomeAffairs.Application.Workflow.WorkItems;

internal sealed class
    GetApplicationWorkItemHistoryQueryHandler(
        IWorkflowWorkItemRepository repository)
    : IRequestHandler<
        GetApplicationWorkItemHistoryQuery,
        IReadOnlyCollection<
            WorkflowWorkItemResult>>
{
    public async Task<
        IReadOnlyCollection<
            WorkflowWorkItemResult>>
        Handle(
            GetApplicationWorkItemHistoryQuery request,
            CancellationToken cancellationToken)
    {
        var items =
            await repository
                .GetHistoryByApplicationAsync(
                    request.ApplicationId,
                    cancellationToken);

        return items
            .Select(
                GetMyWorkItemsQueryHandler.Map)
            .ToArray();
    }
}
