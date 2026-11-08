using MediatR;

namespace HomeAffairs.Application.Applications.SubmitApplication;

public sealed record SubmitApplicationCommand(
    Guid ApplicationId,
    byte[] ExpectedRowVersion)
    : IRequest<ApplicationResult>;
