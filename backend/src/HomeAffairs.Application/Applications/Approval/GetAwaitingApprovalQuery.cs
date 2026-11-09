using MediatR;

namespace HomeAffairs.Application.Applications.Approval;

public sealed record GetAwaitingApprovalQuery
    : IRequest<
        IReadOnlyCollection<ApplicationResult>>;
