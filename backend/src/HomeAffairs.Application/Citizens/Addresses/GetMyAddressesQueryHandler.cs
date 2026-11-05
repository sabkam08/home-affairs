using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Citizens.Addresses;

internal sealed class GetMyAddressesQueryHandler(
    ICitizenRepository citizenRepository,
    IAddressRepository addressRepository,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetMyAddressesQuery,
        IReadOnlyCollection<CitizenAddressResult>>
{
    public async Task<
        IReadOnlyCollection<CitizenAddressResult>>
        Handle(
            GetMyAddressesQuery request,
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

        var results =
            await addressRepository
                .GetByCitizenIdAsync(
                    citizen.CitizenId,
                    cancellationToken);

        return results
            .Select(
                CreateAddressCommandHandler.Map)
            .ToArray();
    }
}
