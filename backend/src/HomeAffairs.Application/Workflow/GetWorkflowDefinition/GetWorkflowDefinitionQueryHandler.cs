using HomeAffairs.Application.Abstractions.Workflow;
using HomeAffairs.Application.Workflow;
using MediatR;

namespace HomeAffairs.Application.Workflow.GetWorkflowDefinition;

internal sealed class GetWorkflowDefinitionQueryHandler(
    IWorkflowDefinitionRepository repository)
    : IRequestHandler<
        GetWorkflowDefinitionQuery,
        WorkflowDefinitionResult>
{
    public async Task<WorkflowDefinitionResult> Handle(
        GetWorkflowDefinitionQuery request,
        CancellationToken cancellationToken)
    {
        var workflow =
            await repository
                .GetActiveByApplicationTypeAsync(
                    request.ApplicationType
                        .Trim()
                        .ToUpperInvariant(),
                    cancellationToken);

        if (workflow is null)
        {
            throw new
                WorkflowDefinitionNotFoundException();
        }

        return new WorkflowDefinitionResult(
            workflow.WorkflowDefinitionId,
            workflow.ApplicationType,
            workflow.Name,
            workflow.Version,
            workflow.IsActive,
            workflow.States
                .Select(state =>
                    new WorkflowStateResult(
                        state.StateCode,
                        state.DisplayName,
                        state.Sequence,
                        state.IsInitial,
                        state.IsTerminal))
                .ToArray(),
            workflow.Transitions
                .Select(transition =>
                    new WorkflowTransitionResult(
                        transition.FromStateCode,
                        transition.ToStateCode,
                        transition.ActionCode,
                        transition.DisplayName,
                        transition.RequiredPermission,
                        transition.RequiresReason))
                .ToArray());
    }
}
