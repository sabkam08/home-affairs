using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Applications.GetApplicationHistory;

internal sealed class GetApplicationHistoryQueryHandler(
    IApplicationRepository applicationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetApplicationHistoryQuery,
        IReadOnlyCollection<
            ApplicationStatusHistoryResult>>
{
    public async Task<
        IReadOnlyCollection<
            ApplicationStatusHistoryResult>>
        Handle(
            GetApplicationHistoryQuery request,
            CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            var history =
                await applicationRepository
                    .GetStatusHistoryForApplicantAsync(
                        request.ApplicationId,
                        currentUser.UserId.Value,
                        cancellationToken);

            return history
                .Select(item =>
                    new ApplicationStatusHistoryResult(
                        item.ApplicationStatusHistoryId,
                        item.ApplicationId,
                        item.FromStatus,
                        item.ToStatus,
                        item.ChangedByUserId,
                        item.Reason,
                        item.ChangedAtUtc))
                .ToArray();
        }
        catch (ApplicationNotFoundPersistenceException)
        {
            throw new ApplicationNotFoundException();
        }
    }
}
