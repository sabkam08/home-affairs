using MediatR;

namespace HomeAffairs.Application.Citizens.Addresses;

public sealed record UpdateAddressCommand(
    Guid AddressId,
    string AddressType,
    string AddressLine1,
    string? AddressLine2,
    string? Suburb,
    string CityOrTown,
    string? Province,
    string? PostalCode,
    string CountryCode,
    bool IsPrimary,
    byte[] ExpectedRowVersion)
    : IRequest<CitizenAddressResult>;
