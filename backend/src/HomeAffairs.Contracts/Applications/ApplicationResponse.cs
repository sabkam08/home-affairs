namespace HomeAffairs.Contracts.Applications;

public sealed record ApplicationResponse(
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
    string RowVersion);
