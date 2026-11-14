using MediatR;

namespace HomeAffairs.Application.SmartId.SaveSmartIdServiceDetails;

public sealed record SaveSmartIdServiceDetailsCommand(
    Guid ApplicationId,
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
    byte[]? ExpectedRowVersion)
    : IRequest<SmartIdServiceDetailsResult>;
