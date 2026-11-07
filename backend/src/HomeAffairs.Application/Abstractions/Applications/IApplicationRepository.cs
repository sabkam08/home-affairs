using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Application.Abstractions.Applications;

public interface IApplicationRepository
{
    Task<HomeAffairsApplication> CreateDraftAsync(
        HomeAffairsApplication application,
        Guid userId,
        CancellationToken cancellationToken);

    Task<HomeAffairsApplication?>
        GetByIdForApplicantAsync(
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HomeAffairsApplication>>
        GetMineAsync(
            Guid userId,
            CancellationToken cancellationToken);
}
