namespace HomeAffairs.Contracts.Applications;

public sealed record ApplicationSearchResponse(
    IReadOnlyCollection<ApplicationResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
