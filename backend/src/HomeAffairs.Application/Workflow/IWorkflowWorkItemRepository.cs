using HomeAffairs.Domain.Workflow;

namespace HomeAffairs.Application.Abstractions.Workflow;

public interface IWorkflowWorkItemRepository
{
    Task<IReadOnlyCollection<WorkflowWorkItem>>
        GetAssignedToUserAsync(
            Guid userId,
            CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WorkflowWorkItem>>
        GetOpenByRoleAsync(
            string roleCode,
            CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WorkflowWorkItem>>
        GetHistoryByApplicationAsync(
            Guid applicationId,
            CancellationToken cancellationToken);
}
