using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Applications.SubmitApplication;

internal sealed class SubmitApplicationCommandHandler(
    IApplicationRepository applicationRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        SubmitApplicationCommand,
        ApplicationResult>
{
    public async Task<ApplicationResult> Handle(
        SubmitApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            var submitted =
                await applicationRepository.SubmitAsync(
                    request.ApplicationId,
                    currentUser.UserId.Value,
                    request.ExpectedRowVersion,
                    clock.UtcNow,
                    cancellationToken);

            return CreateApplicationCommandHandler
                .Map(submitted);
        }
        catch (ApplicationNotFoundPersistenceException)
        {
            throw new ApplicationNotFoundException();
        }
        catch (ApplicationConcurrencyException)
        {
            throw new ApplicationConflictException();
        }
        catch (InvalidApplicationStatePersistenceException)
        {
            throw new InvalidApplicationStateException(
                "Only draft applications may be submitted.");
        }
    }
}
