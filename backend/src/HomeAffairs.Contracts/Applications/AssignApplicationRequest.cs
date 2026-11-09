namespace HomeAffairs.Contracts.Applications;

public sealed record AssignApplicationRequest(
    Guid AssignedToUserId,
    string RowVersion,
    string? Reason);
