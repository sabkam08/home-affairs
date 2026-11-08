namespace HomeAffairs.Application.Applications.GetApplicationHistory;

public sealed record ApplicationStatusHistoryResult(
    Guid ApplicationStatusHistoryId,
    Guid ApplicationId,
    string? FromStatus,
    string ToStatus,
    Guid? ChangedByUserId,
    string? Reason,
    DateTimeOffset ChangedAtUtc);
