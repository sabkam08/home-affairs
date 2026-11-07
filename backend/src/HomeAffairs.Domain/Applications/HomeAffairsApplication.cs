namespace HomeAffairs.Domain.Applications;

public sealed class HomeAffairsApplication
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

    public DateTimeOffset? SubmittedAtUtc { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public bool IsSelfApplication =>
        ApplicantCitizenId ==
        SubjectCitizenId;

    public bool IsDraft =>
        string.Equals(
            Status,
            ApplicationStatuses.Draft,
            StringComparison.Ordinal);
}
