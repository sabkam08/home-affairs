using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Applications.Approval;

internal sealed class DecideApplicationCommandHandler(
    IApplicationRepository applicationRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        DecideApplicationCommand,
        ApplicationResult>
{
    public async Task<ApplicationResult> Handle(
        DecideApplicationCommand request,
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
                await applicationRepository.DecideAsync(
                    request.ApplicationId,
                    currentUser.UserId.Value,
                    request.Decision
                        .Trim()
                        .ToUpperInvariant(),
                    Normalize(request.Reason),
                    request.ExpectedRowVersion,
                    clock.UtcNow,
                    cancellationToken);

            return CreateApplicationCommandHandler.Map(
                application);
        }
        catch (ApplicationNotFoundPersistenceException)
        {
            throw new ApplicationNotFoundException();
        }
        catch (InvalidApplicationStatePersistenceException)
        {
            throw new InvalidApplicationStateException(
                "Only applications awaiting approval " +
                "may be decided.");
        }
        catch (InvalidApplicationDecisionException)
        {
            throw new ApplicationDecisionNotAllowedException(
                "The application decision is invalid.");
        }
        catch (InvalidApplicationDecisionMakerException)
        {
            throw new ApplicationDecisionNotAllowedException(
                "Only an active supervisor may decide " +
                "the application.");
        }
        catch (ApplicationAssignmentRequiredException)
        {
            throw new ApplicationDecisionNotAllowedException(
                "The application does not have an " +
                "active practitioner assignment.");
        }
        catch (SelfApprovalNotAllowedException)
        {
            throw new ApplicationSelfApprovalException();
        }
        catch (ApplicationConcurrencyException)
        {
            throw new ApplicationConflictException();
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
