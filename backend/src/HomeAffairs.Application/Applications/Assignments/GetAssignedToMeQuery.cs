using MediatR;

namespace HomeAffairs.Application.Applications.Assignments;

public sealed record GetAssignedToMeQuery
    : IRequest<IReadOnlyCollection<ApplicationResult>>;
