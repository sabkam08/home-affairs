using HomeAffairs.Application.Abstractions.Applications;
using MediatR;

namespace HomeAffairs.Application.Applications.Assignments;

internal sealed class GetAssignmentHistoryQueryHandler(
    IApplicationAssignmentRepository repository)
    : IRequestHandler<
        GetAssignmentHistoryQuery,
        IReadOnlyCollection<ApplicationAssignmentResult>>
{
    public async Task<
        IReadOnlyCollection<ApplicationAssignmentResult>>
        Handle(
            GetAssignmentHistoryQuery request,
            CancellationToken cancellationToken)
    {
        var history =
            await repository.GetHistoryAsync(
                request.ApplicationId,
                cancellationToken);

        return history
            .Select(
                AssignApplicationCommandHandler.Map)
            .ToArray();
    }
}
