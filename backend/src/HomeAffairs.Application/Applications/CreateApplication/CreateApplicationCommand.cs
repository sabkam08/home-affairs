using MediatR;

namespace HomeAffairs.Application.Applications.CreateApplication;

public sealed record CreateApplicationCommand(
    string ApplicationType,
    Guid? SubjectCitizenId)
    : IRequest<ApplicationResult>;
