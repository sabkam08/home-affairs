using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Workflow;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Workflow.WorkItems;

internal sealed class GetMyWorkItemsQueryHandler(
    IWorkflowWorkItemRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetMyWorkItemsQuery,
        IReadOnlyCollection<
            WorkflowWorkItemResult>>
{
    public async Task<
        IReadOnlyCollection<
            WorkflowWorkItemResult>>
        Handle(
            GetMyWorkItemsQuery request,
            CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var items =
            await repository.GetAssignedToUserAsync(
                currentUser.UserId.Value,
                cancellationToken);

        return items
            .Select(Map)
            .ToArray();
    }

    internal static WorkflowWorkItemResult Map(
        HomeAffairs.Domain.Workflow.WorkflowWorkItem item)
    {
        return new WorkflowWorkItemResult(
            item.WorkItemId,
            item.WorkflowInstanceId,
            item.ApplicationId,
            item.WorkItemType,
            item.Status,
            item.AssignedToUserId,
            item.AssignedRoleCode,
            item.CreatedAtUtc,
            item.ClaimedAtUtc,
            item.CompletedAtUtc,
            item.CancelledAtUtc,
            item.CompletedByUserId,
            item.Reason,
            item.RowVersion);
    }
}
