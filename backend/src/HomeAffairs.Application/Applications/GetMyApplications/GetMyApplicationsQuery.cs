using MediatR;

namespace HomeAffairs.Application.Applications.GetMyApplications;

public sealed record GetMyApplicationsQuery
    : IRequest<
        IReadOnlyCollection<ApplicationResult>>;
