using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Applications.GetMyApplications;

internal sealed class GetMyApplicationsQueryHandler(
    IApplicationRepository applicationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetMyApplicationsQuery,
        IReadOnlyCollection<ApplicationResult>>
{
    public async Task<
        IReadOnlyCollection<ApplicationResult>>
        Handle(
            GetMyApplicationsQuery request,
            CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var applications =
            await applicationRepository
                .GetMineAsync(
                    currentUser.UserId.Value,
                    cancellationToken);

        return applications
            .Select(
                CreateApplicationCommandHandler.Map)
            .ToArray();
    }
}
