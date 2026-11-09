using MediatR;

namespace HomeAffairs.Application.Applications.Assignments;

public sealed record AssignApplicationCommand(
    Guid ApplicationId,
    Guid AssignedToUserId,
    byte[] ExpectedApplicationRowVersion,
    string? Reason)
    : IRequest<ApplicationAssignmentResult>;

public sealed record ApplicationAssignmentResult(
    Guid ApplicationAssignmentId,
    Guid ApplicationId,
    Guid AssignedToUserId,
    Guid AssignedByUserId,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? UnassignedAtUtc,
    Guid? UnassignedByUserId,
    string? Reason,
    bool IsActive,
    byte[] RowVersion);
