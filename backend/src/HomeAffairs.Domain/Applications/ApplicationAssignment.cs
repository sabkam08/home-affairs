namespace HomeAffairs.Domain.Applications;

public sealed class ApplicationAssignment
{
    public Guid ApplicationAssignmentId { get; init; }

    public Guid ApplicationId { get; init; }

    public Guid AssignedToUserId { get; init; }

    public Guid AssignedByUserId { get; init; }

    public DateTimeOffset AssignedAtUtc { get; init; }

    public DateTimeOffset? UnassignedAtUtc { get; init; }

    public Guid? UnassignedByUserId { get; init; }

    public string? Reason { get; init; }

    public bool IsActive { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
