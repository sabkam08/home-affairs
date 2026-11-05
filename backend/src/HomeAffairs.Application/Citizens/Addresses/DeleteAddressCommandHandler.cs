using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using MediatR;

namespace HomeAffairs.Application.Citizens.Addresses;

internal sealed class DeleteAddressCommandHandler(
    IAddressRepository addressRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<DeleteAddressCommand>
{
    public async Task Handle(
        DeleteAddressCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        try
        {
            await addressRepository.DeleteAsync(
                request.AddressId,
                currentUser.UserId.Value,
                request.ExpectedRowVersion,
                clock.UtcNow,
                cancellationToken);
        }
        catch (AddressNotFoundPersistenceException)
        {
            throw new AddressNotFoundException();
        }
        catch (AddressConcurrencyException)
        {
            throw new AddressConflictException();
        }
    }
}
