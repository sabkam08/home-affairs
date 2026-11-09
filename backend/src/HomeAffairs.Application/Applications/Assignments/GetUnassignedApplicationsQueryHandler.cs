using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Applications.CreateApplication;
using MediatR;

namespace HomeAffairs.Application.Applications.Assignments;

internal sealed class GetUnassignedApplicationsQueryHandler(
    IApplicationAssignmentRepository repository)
    : IRequestHandler<
        GetUnassignedApplicationsQuery,
        IReadOnlyCollection<ApplicationResult>>
{
    public async Task<IReadOnlyCollection<ApplicationResult>>
        Handle(
            GetUnassignedApplicationsQuery request,
            CancellationToken cancellationToken)
    {
        var applications =
            await repository.GetUnassignedAsync(
                cancellationToken);

        return applications
            .Select(
                CreateApplicationCommandHandler.Map)
            .ToArray();
    }
}
