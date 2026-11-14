using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.SmartId;
using HomeAffairs.Application.SmartId.SaveSmartIdServiceDetails;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.SmartId.GetSmartIdServiceDetails;

internal sealed class GetSmartIdServiceDetailsQueryHandler(
    ISmartIdServiceRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetSmartIdServiceDetailsQuery,
        SmartIdServiceDetailsResult>
{
    public async Task<SmartIdServiceDetailsResult> Handle(
        GetSmartIdServiceDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var details =
            await repository.GetForApplicantAsync(
                request.ApplicationId,
                currentUser.UserId.Value,
                cancellationToken);

        if (details is null)
        {
            throw new
                SmartIdServiceApplicationNotFoundException();
        }

        return SaveSmartIdServiceDetailsCommandHandler.Map(
            details);
    }
}
