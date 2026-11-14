namespace HomeAffairs.Contracts.SmartId;

public sealed record SaveSmartIdServiceDetailsRequest(
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
    string? RowVersion);
