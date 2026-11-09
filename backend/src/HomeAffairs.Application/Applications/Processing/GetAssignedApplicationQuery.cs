using MediatR;

namespace HomeAffairs.Application.Applications.Processing;

public sealed record GetAssignedApplicationQuery(
    Guid ApplicationId)
    : IRequest<ApplicationResult>;
