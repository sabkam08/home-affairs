using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Infrastructure.Citizens;

internal sealed class AddressRecord
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
        string.Empty;

    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public CitizenAddress ToDomain()
    {
        return new CitizenAddress
        {
            AddressId = AddressId,
            CitizenId = CitizenId,
            AddressType = AddressType,
            AddressLine1 = AddressLine1,
            AddressLine2 = AddressLine2,
            Suburb = Suburb,
            CityOrTown = CityOrTown,
            Province = Province,
            PostalCode = PostalCode,
            CountryCode = CountryCode,
            IsPrimary = IsPrimary,
            IsActive = IsActive,

            CreatedAtUtc =
                ToUtc(CreatedAtUtc),

            UpdatedAtUtc =
                UpdatedAtUtc.HasValue
                    ? ToUtc(UpdatedAtUtc.Value)
                    : null,

            RowVersion = RowVersion
        };
    }

    private static DateTimeOffset ToUtc(
        DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc));
    }
}
