namespace HomeAffairs.Contracts.SmartId;

public sealed record SmartIdServiceDetailsResponse(
    Guid SmartIdServiceDetailsId,
    Guid ApplicationId,
    string ApplicationType,
    string? ExistingSmartIdNumber,
    string? RequestedFirstName,
    string? RequestedMiddleNames,
    string? RequestedLastName,
    DateOnly? RequestedDateOfBirth,
    string? RequestedSex,
    string? ReplacementReason,
    DateOnly? IncidentDate,
    string? PoliceCaseNumber,
    string? PoliceStation,
    string? DamageDescription,
    string? CorrectionField,
    string? CurrentValue,
    string? RequestedValue,
    string? RequestReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string RowVersion);
