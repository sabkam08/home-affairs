using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Citizens;
using MediatR;

namespace HomeAffairs.Application.Citizens.Addresses;

internal sealed class UpdateAddressCommandHandler(
    IAddressRepository addressRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        UpdateAddressCommand,
        CitizenAddressResult>
{
    public async Task<CitizenAddressResult> Handle(
        UpdateAddressCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new DomainException(
                "The current user could not be determined.");
        }

        var userId =
            currentUser.UserId.Value;

        var existing =
            await addressRepository
                .GetByIdForUserAsync(
                    request.AddressId,
                    userId,
                    cancellationToken);

        if (existing is null)
        {
            throw new AddressNotFoundException();
        }

        var updated =
            new CitizenAddress
            {
                AddressId = existing.AddressId,
                CitizenId = existing.CitizenId,

                AddressType =
                    request.AddressType
                        .Trim()
                        .ToUpperInvariant(),

                AddressLine1 =
                    request.AddressLine1.Trim(),

                AddressLine2 =
                    CreateAddressCommandHandler.Normalize(
                        request.AddressLine2),

                Suburb =
                    CreateAddressCommandHandler.Normalize(
                        request.Suburb),

                CityOrTown =
                    request.CityOrTown.Trim(),

                Province =
                    CreateAddressCommandHandler.Normalize(
                        request.Province),

                PostalCode =
                    CreateAddressCommandHandler.Normalize(
                        request.PostalCode),

                CountryCode =
                    request.CountryCode
                        .Trim()
                        .ToUpperInvariant(),

                IsPrimary = request.IsPrimary,
                IsActive = true,
                CreatedAtUtc =
                    existing.CreatedAtUtc,
                UpdatedAtUtc =
                    clock.UtcNow
            };

        try
        {
            var persisted =
                await addressRepository.UpdateAsync(
                    updated,
                    userId,
                    request.ExpectedRowVersion,
                    cancellationToken);

            return CreateAddressCommandHandler
                .Map(persisted);
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
