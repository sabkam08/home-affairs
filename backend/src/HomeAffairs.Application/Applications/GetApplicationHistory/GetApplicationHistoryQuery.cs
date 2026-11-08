using MediatR;

namespace HomeAffairs.Application.Applications.GetApplicationHistory;

public sealed record GetApplicationHistoryQuery(
    Guid ApplicationId)
    : IRequest<
        IReadOnlyCollection<
            ApplicationStatusHistoryResult>>;
