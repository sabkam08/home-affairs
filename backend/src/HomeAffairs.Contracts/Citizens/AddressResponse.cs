namespace HomeAffairs.Contracts.Citizens;

public sealed record AddressResponse(
    Guid AddressId,
    Guid CitizenId,
    string AddressType,
    string AddressLine1,
    string? AddressLine2,
    string? Suburb,
    string CityOrTown,
    string? Province,
    string? PostalCode,
    string CountryCode,
    bool IsPrimary,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);
