namespace HomeAffairs.Contracts.Applications;

public sealed record ApplicationAssignmentResponse(
    Guid ApplicationAssignmentId,
    Guid ApplicationId,
    Guid AssignedToUserId,
    Guid AssignedByUserId,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? UnassignedAtUtc,
    Guid? UnassignedByUserId,
    string? Reason,
    bool IsActive,
    string RowVersion);
