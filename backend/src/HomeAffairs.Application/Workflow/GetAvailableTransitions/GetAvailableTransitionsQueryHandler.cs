using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Workflow;
using HomeAffairs.Application.Workflow;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Workflow.GetAvailableTransitions;

internal sealed class GetAvailableTransitionsQueryHandler(
    IWorkflowInstanceRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetAvailableTransitionsQuery,
        IReadOnlyCollection<
            AvailableWorkflowTransitionResult>>
{
    public async Task<
        IReadOnlyCollection<
            AvailableWorkflowTransitionResult>>
        Handle(
            GetAvailableTransitionsQuery request,
            CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        /*
            Verify applicant ownership before exposing the
            application's workflow-transition metadata.
        */
        var instance =
            await repository.GetForApplicantAsync(
                request.ApplicationId,
                currentUser.UserId.Value,
                cancellationToken);

        if (instance is null)
        {
            throw new WorkflowInstanceNotFoundException();
        }

        var transitions =
            await repository.GetAvailableTransitionsAsync(
                request.ApplicationId,
                cancellationToken);

        return transitions
            .Select(transition =>
                new AvailableWorkflowTransitionResult(
                    transition.FromStateCode,
                    transition.ToStateCode,
                    transition.ActionCode,
                    transition.DisplayName,
                    transition.RequiredPermission,
                    transition.RequiresReason))
            .ToArray();
    }
}
