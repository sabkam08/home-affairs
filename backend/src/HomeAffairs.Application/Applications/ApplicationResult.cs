namespace HomeAffairs.Application.Applications;

public sealed record ApplicationResult(
    Guid ApplicationId,
    string ApplicationNumber,
    string ApplicationType,
    Guid ApplicantCitizenId,
    Guid SubjectCitizenId,
    string Status,
    bool IsSelfApplication,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    byte[] RowVersion);
