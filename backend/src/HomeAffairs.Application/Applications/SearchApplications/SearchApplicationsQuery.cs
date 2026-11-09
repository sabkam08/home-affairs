using MediatR;

namespace HomeAffairs.Application.Applications.SearchApplications;

public sealed record SearchApplicationsQuery(
    string? ApplicationNumber,
    string? ApplicationType,
    string? Status,
    Guid? ApplicantCitizenId,
    Guid? SubjectCitizenId,
    Guid? AssignedToUserId,
    DateTimeOffset? CreatedFromUtc,
    DateTimeOffset? CreatedToUtc,
    int PageNumber,
    int PageSize)
    : IRequest<SearchApplicationsResult>;

public sealed record SearchApplicationsResult(
    IReadOnlyCollection<ApplicationResult> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
