namespace HomeAffairs.Domain.Citizens;

public sealed class CitizenAddress
{
    public Guid AddressId { get; init; }

    public Guid CitizenId { get; init; }

    public string AddressType { get; init; } =
        string.Empty;

    public string AddressLine1 { get; init; } =
        string.Empty;

    public string? AddressLine2 { get; init; }

    public string? Suburb { get; init; }

    public string CityOrTown { get; init; } =
        string.Empty;

    public string? Province { get; init; }

    public string? PostalCode { get; init; }

    public string CountryCode { get; init; } =
        "ZA";

    public bool IsPrimary { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
