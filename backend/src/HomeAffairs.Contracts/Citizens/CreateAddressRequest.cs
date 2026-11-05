namespace HomeAffairs.Contracts.Citizens;

public sealed record CreateAddressRequest(
    string AddressType,
    string AddressLine1,
    string? AddressLine2,
    string? Suburb,
    string CityOrTown,
    string? Province,
    string? PostalCode,
    string CountryCode,
    bool IsPrimary);
