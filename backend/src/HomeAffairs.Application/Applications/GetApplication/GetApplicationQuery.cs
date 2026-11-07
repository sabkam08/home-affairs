using MediatR;

namespace HomeAffairs.Application.Applications.GetApplication;

public sealed record GetApplicationQuery(
    Guid ApplicationId)
    : IRequest<ApplicationResult>;
