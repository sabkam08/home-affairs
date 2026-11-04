using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Citizens.GetMyProfile;

internal sealed class GetMyCitizenProfileQueryHandler(
    ICitizenRepository citizenRepository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetMyCitizenProfileQuery,
        GetMyCitizenProfileResult>
{
    public async Task<GetMyCitizenProfileResult> Handle(
        GetMyCitizenProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var citizen =
            await citizenRepository.GetByUserIdAsync(
                currentUser.UserId.Value,
                cancellationToken);

        if (citizen is null)
        {
            throw new CitizenProfileNotFoundException();
        }

        return new GetMyCitizenProfileResult(
            citizen.CitizenId,
            citizen.SouthAfricanIdNumber,
            citizen.FirstName,
            citizen.MiddleNames,
            citizen.LastName,
            citizen.DateOfBirth,
            citizen.Sex,
            citizen.IsActive,
            citizen.CreatedAtUtc,
            citizen.UpdatedAtUtc,
            citizen.RowVersion);
    }
}
