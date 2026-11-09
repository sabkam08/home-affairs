using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Applications.Processing;

internal sealed class GetAssignedApplicationQueryHandler(
    IApplicationRepository applicationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetAssignedApplicationQuery,
        ApplicationResult>
{
    public async Task<ApplicationResult> Handle(
        GetAssignedApplicationQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var application =
            await applicationRepository
                .GetAssignedByIdAsync(
                    request.ApplicationId,
                    currentUser.UserId.Value,
                    cancellationToken);

        if (application is null)
        {
            throw new ApplicationNotFoundException();
        }

        return CreateApplicationCommandHandler.Map(
            application);
    }
}
