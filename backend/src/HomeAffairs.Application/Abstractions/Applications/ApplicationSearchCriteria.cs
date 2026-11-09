namespace HomeAffairs.Application.Abstractions.Applications;

public sealed record ApplicationSearchCriteria(
    string? ApplicationNumber,
    string? ApplicationType,
    string? Status,
    Guid? ApplicantCitizenId,
    Guid? SubjectCitizenId,
    Guid? AssignedToUserId,
    DateTimeOffset? CreatedFromUtc,
    DateTimeOffset? CreatedToUtc,
    int PageNumber,
    int PageSize);
