using MediatR;

namespace HomeAffairs.Application.Applications.CancelApplication;

public sealed record CancelApplicationCommand(
    Guid ApplicationId,
    byte[] ExpectedRowVersion,
    string? Reason)
    : IRequest<ApplicationResult>;
