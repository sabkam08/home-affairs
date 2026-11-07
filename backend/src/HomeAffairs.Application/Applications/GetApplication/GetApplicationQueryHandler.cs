using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Applications.GetApplication;

internal sealed class GetApplicationQueryHandler(
    IApplicationRepository applicationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetApplicationQuery,
        ApplicationResult>
{
    public async Task<ApplicationResult> Handle(
        GetApplicationQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var application =
            await applicationRepository
                .GetByIdForApplicantAsync(
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
