using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Applications.CancelApplication;

internal sealed class CancelApplicationCommandHandler(
    IApplicationRepository applicationRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        CancelApplicationCommand,
        ApplicationResult>
{
    public async Task<ApplicationResult> Handle(
        CancelApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            var cancelled =
                await applicationRepository
                    .CancelDraftAsync(
                        request.ApplicationId,
                        currentUser.UserId.Value,
                        request.ExpectedRowVersion,
                        Normalize(request.Reason),
                        clock.UtcNow,
                        cancellationToken);

            return CreateApplicationCommandHandler
                .Map(cancelled);
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
                "Only draft applications may be cancelled.");
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
