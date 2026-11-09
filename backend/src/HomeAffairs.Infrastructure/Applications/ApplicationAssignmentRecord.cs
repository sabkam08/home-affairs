using HomeAffairs.Domain.Applications;

namespace HomeAffairs.Infrastructure.Applications;

internal sealed class ApplicationAssignmentRecord
{
    public Guid ApplicationAssignmentId { get; init; }

    public Guid ApplicationId { get; init; }

    public Guid AssignedToUserId { get; init; }

    public Guid AssignedByUserId { get; init; }

    public DateTime AssignedAtUtc { get; init; }

    public DateTime? UnassignedAtUtc { get; init; }

    public Guid? UnassignedByUserId { get; init; }

    public string? Reason { get; init; }

    public bool IsActive { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public ApplicationAssignment ToDomain()
    {
        return new ApplicationAssignment
        {
            ApplicationAssignmentId =
                ApplicationAssignmentId,

            ApplicationId =
                ApplicationId,

            AssignedToUserId =
                AssignedToUserId,

            AssignedByUserId =
                AssignedByUserId,

            AssignedAtUtc =
                ToUtc(AssignedAtUtc),

            UnassignedAtUtc =
                UnassignedAtUtc.HasValue
                    ? ToUtc(UnassignedAtUtc.Value)
                    : null,

            UnassignedByUserId =
                UnassignedByUserId,

            Reason =
                Reason,

            IsActive =
                IsActive,

            RowVersion =
                RowVersion
        };
    }

    private static DateTimeOffset ToUtc(
        DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc));
    }
}
