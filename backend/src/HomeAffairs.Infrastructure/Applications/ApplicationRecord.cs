using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Infrastructure.Applications;

internal sealed class ApplicationRecord
{
    public Guid ApplicationId { get; init; }

    public string ApplicationNumber { get; init; } =
        string.Empty;

    public string ApplicationType { get; init; } =
        string.Empty;

    public Guid ApplicantCitizenId { get; init; }

    public Guid SubjectCitizenId { get; init; }

    public string Status { get; init; } =
        string.Empty;

    public DateTime? SubmittedAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public HomeAffairsApplication ToDomain()
    {
        return new HomeAffairsApplication
        {
            ApplicationId =
                ApplicationId,

            ApplicationNumber =
                ApplicationNumber,

            ApplicationType =
                ApplicationType,

            ApplicantCitizenId =
                ApplicantCitizenId,

            SubjectCitizenId =
                SubjectCitizenId,

            Status =
                Status,

            SubmittedAtUtc =
                SubmittedAtUtc.HasValue
                    ? ToUtc(
                        SubmittedAtUtc.Value)
                    : null,

            CreatedAtUtc =
                ToUtc(CreatedAtUtc),

            UpdatedAtUtc =
                UpdatedAtUtc.HasValue
                    ? ToUtc(
                        UpdatedAtUtc.Value)
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
