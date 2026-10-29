namespace HomeAffairs.Application.Abstractions.Identity;

public sealed record SecurityEvent(
    Guid SecurityEventId,
    Guid? UserId,
    string EventType,
    string? Description,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId,
    DateTimeOffset OccurredAtUtc);
