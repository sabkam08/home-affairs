using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Application.Abstractions.Applications;

public interface IApplicationAssignmentRepository
{
    Task<ApplicationAssignment> AssignAsync(
        Guid applicationId,
        Guid assignedToUserId,
        Guid assignedByUserId,
        string? reason,
        byte[] expectedApplicationRowVersion,
        DateTimeOffset assignedAtUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ApplicationAssignment>>
        GetHistoryAsync(
            Guid applicationId,
            CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HomeAffairsApplication>>
        GetAssignedToMeAsync(
            Guid userId,
            CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HomeAffairsApplication>>
        GetUnassignedAsync(
            CancellationToken cancellationToken);
}
