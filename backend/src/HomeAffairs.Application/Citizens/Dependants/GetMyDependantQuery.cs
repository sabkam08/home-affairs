using MediatR;

namespace HomeAffairs.Application.Citizens.Dependants;

public sealed record GetMyDependantQuery(
    Guid SubjectCitizenId)
    : IRequest<DependantResult>;
