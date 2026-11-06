using MediatR;

namespace HomeAffairs.Application.Citizens.Dependants;

public sealed record GetMyDependantsQuery
    : IRequest<IReadOnlyCollection<DependantResult>>;
