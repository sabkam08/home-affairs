using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Applications.CreateApplication;
using MediatR;

namespace HomeAffairs.Application.Applications.SearchApplications;

internal sealed class SearchApplicationsQueryHandler(
    IApplicationRepository applicationRepository)
    : IRequestHandler<
        SearchApplicationsQuery,
        SearchApplicationsResult>
{
    public async Task<SearchApplicationsResult> Handle(
        SearchApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var criteria =
            new ApplicationSearchCriteria(
                Normalize(
                    request.ApplicationNumber),

                NormalizeUpper(
                    request.ApplicationType),

                NormalizeUpper(
                    request.Status),

                request.ApplicantCitizenId,
                request.SubjectCitizenId,
                request.AssignedToUserId,
                request.CreatedFromUtc,
                request.CreatedToUtc,
                request.PageNumber,
                request.PageSize);

        var page =
            await applicationRepository.SearchAsync(
                criteria,
                cancellationToken);

        var items =
            page.Items
                .Select(
                    CreateApplicationCommandHandler.Map)
                .ToArray();

        return new SearchApplicationsResult(
            items,
            page.TotalCount,
            page.PageNumber,
            page.PageSize,
            page.TotalPages);
    }

    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeUpper(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value
                .Trim()
                .ToUpperInvariant();
    }
}
