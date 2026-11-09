using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Application.Abstractions.Applications;

public sealed record ApplicationSearchPage(
    IReadOnlyCollection<HomeAffairsApplication> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages =>
        TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                TotalCount /
                (double)PageSize);
}
