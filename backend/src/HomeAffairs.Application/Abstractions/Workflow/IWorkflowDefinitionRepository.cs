using HomeAffairs.Domain.Workflow;

namespace HomeAffairs.Application.Abstractions.Workflow;

public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?>
        GetActiveByApplicationTypeAsync(
            string applicationType,
            CancellationToken cancellationToken);

    Task<bool> IsTransitionAllowedAsync(
        string applicationType,
        string fromState,
        string toState,
        CancellationToken cancellationToken);
}
