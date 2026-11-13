using HomeAffairs.Domain.Birth;

namespace HomeAffairs.Infrastructure.Birth;

internal sealed class BirthServiceDetailsRecord
{
    public Guid BirthServiceDetailsId { get; init; }

    public Guid ApplicationId { get; init; }

    public string ApplicationType { get; init; } =
        string.Empty;

    public string? ChildFirstName { get; init; }

    public string? ChildMiddleNames { get; init; }

    public string? ChildLastName { get; init; }

    public DateTime? ChildDateOfBirth { get; init; }

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

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public BirthServiceDetails ToDomain()
    {
        return new BirthServiceDetails
        {
            BirthServiceDetailsId =
                BirthServiceDetailsId,

            ApplicationId =
                ApplicationId,

            ApplicationType =
                ApplicationType,

            ChildFirstName =
                ChildFirstName,

            ChildMiddleNames =
                ChildMiddleNames,

            ChildLastName =
                ChildLastName,

            ChildDateOfBirth =
                ChildDateOfBirth.HasValue
                    ? DateOnly.FromDateTime(
                        ChildDateOfBirth.Value)
                    : null,

            ChildSex =
                ChildSex,

            PlaceOfBirth =
                PlaceOfBirth,

            ProvinceOfBirth =
                ProvinceOfBirth,

            CountryOfBirth =
                CountryOfBirth,

            MotherFirstName =
                MotherFirstName,

            MotherLastName =
                MotherLastName,

            MotherIdNumber =
                MotherIdNumber,

            FatherFirstName =
                FatherFirstName,

            FatherLastName =
                FatherLastName,

            FatherIdNumber =
                FatherIdNumber,

            CurrentCertificateNumber =
                CurrentCertificateNumber,

            RequestReason =
                RequestReason,

            CreatedAtUtc =
                ToUtc(CreatedAtUtc),

            UpdatedAtUtc =
                UpdatedAtUtc.HasValue
                    ? ToUtc(UpdatedAtUtc.Value)
                    : null,

            RowVersion =
                RowVersion
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
