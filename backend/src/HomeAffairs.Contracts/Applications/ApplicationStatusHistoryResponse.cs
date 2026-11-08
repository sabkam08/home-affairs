namespace HomeAffairs.Contracts.Applications;

public sealed record ApplicationStatusHistoryResponse(
    Guid ApplicationStatusHistoryId,
    Guid ApplicationId,
    string? FromStatus,
    string ToStatus,
    Guid? ChangedByUserId,
    string? Reason,
    DateTimeOffset ChangedAtUtc);
