using MediatR;

namespace HomeAffairs.Application.Applications.Approval;

public sealed record DecideApplicationCommand(
    Guid ApplicationId,
    string Decision,
    byte[] ExpectedRowVersion,
    string? Reason)
    : IRequest<ApplicationResult>;
