using MediatR;

namespace HomeAffairs.Application.Birth.GetBirthServiceDetails;

public sealed record GetBirthServiceDetailsQuery(
    Guid ApplicationId)
    : IRequest<BirthServiceDetailsResult>;
