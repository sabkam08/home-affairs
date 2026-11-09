using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Applications.CreateApplication;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Applications.Assignments;

internal sealed class GetAssignedToMeQueryHandler(
    IApplicationAssignmentRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetAssignedToMeQuery,
        IReadOnlyCollection<ApplicationResult>>
{
    public async Task<IReadOnlyCollection<ApplicationResult>>
        Handle(
            GetAssignedToMeQuery request,
            CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var applications =
            await repository.GetAssignedToMeAsync(
                currentUser.UserId.Value,
                cancellationToken);

        return applications
            .Select(
                CreateApplicationCommandHandler.Map)
            .ToArray();
    }
}
