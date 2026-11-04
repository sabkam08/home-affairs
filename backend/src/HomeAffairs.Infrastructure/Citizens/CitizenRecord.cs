using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Infrastructure.Citizens;

internal sealed class CitizenRecord
{
    public Guid CitizenId { get; init; }

    public Guid? UserId { get; init; }

    public string? SouthAfricanIdNumber { get; init; }

    public string FirstName { get; init; } =
        string.Empty;

    public string? MiddleNames { get; init; }

    public string LastName { get; init; } =
        string.Empty;

    public DateTime DateOfBirth { get; init; }

    public string Sex { get; init; } =
        string.Empty;

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public Citizen ToDomain()
    {
        return new Citizen
        {
            CitizenId = CitizenId,
            UserId = UserId,
            SouthAfricanIdNumber =
                SouthAfricanIdNumber,
            FirstName = FirstName,
            MiddleNames = MiddleNames,
            LastName = LastName,
            DateOfBirth =
                DateOnly.FromDateTime(
                    DateOfBirth),
            Sex = Sex,
            IsActive = IsActive,
            CreatedAtUtc =
                ToUtc(CreatedAtUtc),
            UpdatedAtUtc =
                UpdatedAtUtc.HasValue
                    ? ToUtc(
                        UpdatedAtUtc.Value)
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
