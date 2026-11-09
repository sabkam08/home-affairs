using MediatR;

namespace HomeAffairs.Application.Applications.Assignments;

public sealed record GetUnassignedApplicationsQuery
    : IRequest<IReadOnlyCollection<ApplicationResult>>;
