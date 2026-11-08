using MediatR;

namespace HomeAffairs.Application.Applications.UpdateApplication;

public sealed record UpdateApplicationCommand(
    Guid ApplicationId,
    string ApplicationType,
    Guid? SubjectCitizenId,
    byte[] ExpectedRowVersion)
    : IRequest<ApplicationResult>;
