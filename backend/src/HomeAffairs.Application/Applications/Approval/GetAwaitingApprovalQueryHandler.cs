using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Applications.CreateApplication;
using MediatR;

namespace HomeAffairs.Application.Applications.Approval;

internal sealed class GetAwaitingApprovalQueryHandler(
    IApplicationRepository applicationRepository)
    : IRequestHandler<
        GetAwaitingApprovalQuery,
        IReadOnlyCollection<ApplicationResult>>
{
    public async Task<
        IReadOnlyCollection<ApplicationResult>>
        Handle(
            GetAwaitingApprovalQuery request,
            CancellationToken cancellationToken)
    {
        var applications =
            await applicationRepository
                .GetAwaitingApprovalAsync(
                    cancellationToken);

        return applications
            .Select(
                CreateApplicationCommandHandler.Map)
            .ToArray();
    }
}
