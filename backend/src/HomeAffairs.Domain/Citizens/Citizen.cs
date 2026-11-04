namespace HomeAffairs.Domain.Citizens;

public sealed class Citizen
{
    public Guid CitizenId { get; init; }

    public Guid? UserId { get; init; }

    public string? SouthAfricanIdNumber { get; init; }

    public string FirstName { get; init; } =
        string.Empty;

    public string? MiddleNames { get; init; }

    public string LastName { get; init; } =
        string.Empty;

    public DateOnly DateOfBirth { get; init; }

    public string Sex { get; init; } =
        string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public string FullName
    {
        get
        {
            var components =
                new[]
                {
                    FirstName,
                    MiddleNames,
                    LastName
                }
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value));

            return string.Join(
                " ",
                components);
        }
    }

    public int CalculateAge(
        DateOnly onDate)
    {
        var age =
            onDate.Year -
            DateOfBirth.Year;

        if (onDate <
            DateOfBirth.AddYears(age))
        {
            age--;
        }

        return age;
    }

    public bool IsMinor(
        DateOnly onDate)
    {
        return CalculateAge(onDate) < 18;
    }
}
