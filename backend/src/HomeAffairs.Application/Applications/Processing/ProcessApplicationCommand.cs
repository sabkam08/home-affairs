using MediatR;

namespace HomeAffairs.Application.Applications.Processing;

public sealed record ProcessApplicationCommand(
    Guid ApplicationId,
    string TargetStatus,
    byte[] ExpectedRowVersion,
    string? Reason)
    : IRequest<ApplicationResult>;
