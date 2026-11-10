using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Application.Abstractions.Workflow;
using HomeAffairs.Application.Workflow;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Applications.Processing;

internal sealed class ProcessApplicationCommandHandler(
    IApplicationRepository applicationRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        ProcessApplicationCommand,
        ApplicationResult>
{
    public async Task<ApplicationResult> Handle(
        ProcessApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            var application =
                await applicationRepository
                    .TransitionByAssignedPractitionerAsync(
                        request.ApplicationId,
                        currentUser.UserId.Value,
                        request.TargetStatus
                            .Trim()
                            .ToUpperInvariant(),
                        Normalize(request.Reason),
                        request.ExpectedRowVersion,
                        clock.UtcNow,
                        cancellationToken);

            return CreateApplicationCommandHandler
                .Map(application);
        }
        catch (ApplicationNotFoundPersistenceException)
        {
            /*
                404 intentionally hides whether the
                application exists but belongs to another
                practitioner.
            */
            throw new ApplicationNotFoundException();
        }
        catch (InvalidApplicationStatePersistenceException)
        {
            throw new InvalidApplicationStateException(
                "The requested application processing " +
                "transition is not valid.");
        }
        catch (ApplicationConcurrencyException)
        {
            throw new ApplicationConflictException();
        }
        catch (WorkflowStateMismatchPersistenceException)
        {
            throw new WorkflowStateMismatchException();
        }
    }

    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
