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

    Task<HomeAffairsApplication> UpdateDraftAsync(
        Guid applicationId,
        Guid userId,
        string applicationType,
        Guid subjectCitizenId,
        byte[] expectedRowVersion,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken);

    Task<HomeAffairsApplication> SubmitAsync(
        Guid applicationId,
        Guid userId,
        byte[] expectedRowVersion,
        DateTimeOffset submittedAtUtc,
        CancellationToken cancellationToken);

    Task<HomeAffairsApplication> CancelDraftAsync(
        Guid applicationId,
        Guid userId,
        byte[] expectedRowVersion,
        string? reason,
        DateTimeOffset cancelledAtUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ApplicationStatusHistory>>
        GetStatusHistoryForApplicantAsync(
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken);

    Task<HomeAffairsApplication?>
    GetAssignedByIdAsync(
        Guid applicationId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<HomeAffairsApplication>
        TransitionByAssignedPractitionerAsync(
            Guid applicationId,
            Guid userId,
            string targetStatus,
            string? reason,
            byte[] expectedRowVersion,
            DateTimeOffset changedAtUtc,
            CancellationToken cancellationToken);

    Task<HomeAffairsApplication> DecideAsync(
        Guid applicationId,
        Guid decisionByUserId,
        string decision,
        string? reason,
        byte[] expectedRowVersion,
        DateTimeOffset decidedAtUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HomeAffairsApplication>>
        GetAwaitingApprovalAsync(
            CancellationToken cancellationToken);
}
