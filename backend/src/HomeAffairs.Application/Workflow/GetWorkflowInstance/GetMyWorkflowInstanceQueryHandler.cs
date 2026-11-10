using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Workflow;
using HomeAffairs.Application.Workflow;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Workflow.GetWorkflowInstance;

internal sealed class GetMyWorkflowInstanceQueryHandler(
    IWorkflowInstanceRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetMyWorkflowInstanceQuery,
        WorkflowInstanceResult>
{
    public async Task<WorkflowInstanceResult> Handle(
        GetMyWorkflowInstanceQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var instance =
            await repository.GetForApplicantAsync(
                request.ApplicationId,
                currentUser.UserId.Value,
                cancellationToken);

        if (instance is null)
        {
            throw new WorkflowInstanceNotFoundException();
        }

        return Map(instance);
    }

    internal static WorkflowInstanceResult Map(
        HomeAffairs.Domain.Workflow.WorkflowInstance instance)
    {
        return new WorkflowInstanceResult(
            instance.WorkflowInstanceId,
            instance.WorkflowDefinitionId,
            instance.ApplicationId,
            instance.ApplicationType,
            instance.WorkflowName,
            instance.WorkflowVersion,
            instance.CurrentStateCode,
            instance.StartedAtUtc,
            instance.CompletedAtUtc,
            instance.CreatedAtUtc,
            instance.UpdatedAtUtc,
            instance.RowVersion);
    }
}
