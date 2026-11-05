using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Common.Exceptions;
using HomeAffairs.Domain.Citizens;
using MediatR;

namespace HomeAffairs.Application.Citizens.Addresses;

internal sealed class CreateAddressCommandHandler(
    ICitizenRepository citizenRepository,
    IAddressRepository addressRepository,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<
        CreateAddressCommand,
        CitizenAddressResult>
{
    public async Task<CitizenAddressResult> Handle(
        CreateAddressCommand request,
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

        var address =
            new CitizenAddress
            {
                AddressId = Guid.NewGuid(),
                CitizenId = citizen.CitizenId,

                AddressType =
                    request.AddressType
                        .Trim()
                        .ToUpperInvariant(),

                AddressLine1 =
                    request.AddressLine1.Trim(),

                AddressLine2 =
                    Normalize(request.AddressLine2),

                Suburb =
                    Normalize(request.Suburb),

                CityOrTown =
                    request.CityOrTown.Trim(),

                Province =
                    Normalize(request.Province),

                PostalCode =
                    Normalize(request.PostalCode),

                CountryCode =
                    request.CountryCode
                        .Trim()
                        .ToUpperInvariant(),

                IsPrimary =
                    request.IsPrimary,

                IsActive = true,

                CreatedAtUtc =
                    clock.UtcNow
            };

        var created =
            await addressRepository.CreateAsync(
                address,
                currentUser.UserId.Value,
                cancellationToken);

        return Map(created);
    }

    internal static CitizenAddressResult Map(
        CitizenAddress address)
    {
        return new CitizenAddressResult(
            address.AddressId,
            address.CitizenId,
            address.AddressType,
            address.AddressLine1,
            address.AddressLine2,
            address.Suburb,
            address.CityOrTown,
            address.Province,
            address.PostalCode,
            address.CountryCode,
            address.IsPrimary,
            address.IsActive,
            address.CreatedAtUtc,
            address.UpdatedAtUtc,
            address.RowVersion);
    }

    internal static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
