namespace HomeAffairs.Domain.Birth;

public sealed class BirthServiceDetails
{
    public Guid BirthServiceDetailsId { get; init; }

    public Guid ApplicationId { get; init; }

    public string ApplicationType { get; init; } =
        string.Empty;

    public string? ChildFirstName { get; init; }

    public string? ChildMiddleNames { get; init; }

    public string? ChildLastName { get; init; }

    public DateOnly? ChildDateOfBirth { get; init; }

    public string? ChildSex { get; init; }

    public string? PlaceOfBirth { get; init; }

    public string? ProvinceOfBirth { get; init; }

    public string? CountryOfBirth { get; init; }

    public string? MotherFirstName { get; init; }

    public string? MotherLastName { get; init; }

    public string? MotherIdNumber { get; init; }

    public string? FatherFirstName { get; init; }

    public string? FatherLastName { get; init; }

    public string? FatherIdNumber { get; init; }

    public string? CurrentCertificateNumber { get; init; }

    public string? RequestReason { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
