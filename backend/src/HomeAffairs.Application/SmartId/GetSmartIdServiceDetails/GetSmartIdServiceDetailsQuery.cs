using MediatR;

namespace HomeAffairs.Application.SmartId.GetSmartIdServiceDetails;

public sealed record GetSmartIdServiceDetailsQuery(
    Guid ApplicationId)
    : IRequest<SmartIdServiceDetailsResult>;
