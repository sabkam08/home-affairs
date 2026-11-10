using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Application.Abstractions.Workflow;
using HomeAffairs.Domain.Workflow;

namespace HomeAffairs.Infrastructure.Workflow;

internal sealed class WorkflowWorkItemRepository(
    ISqlConnectionFactory connectionFactory)
    : IWorkflowWorkItemRepository
{
    public async Task<
        IReadOnlyCollection<WorkflowWorkItem>>
        GetAssignedToUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        return await QueryAsync(
            "[workflow].WorkItem_GetAssignedToUser",
            new
            {
                UserId = userId
            },
            cancellationToken);
    }

    public async Task<
        IReadOnlyCollection<WorkflowWorkItem>>
        GetOpenByRoleAsync(
            string roleCode,
            CancellationToken cancellationToken)
    {
        return await QueryAsync(
            "[workflow].WorkItem_GetOpenByRole",
            new
            {
                RoleCode = roleCode
            },
            cancellationToken);
    }

    public async Task<
        IReadOnlyCollection<WorkflowWorkItem>>
        GetHistoryByApplicationAsync(
            Guid applicationId,
            CancellationToken cancellationToken)
    {
        return await QueryAsync(
            "[workflow].WorkItem_GetHistoryByApplication",
            new
            {
                ApplicationId = applicationId
            },
            cancellationToken);
    }

    private async Task<
        IReadOnlyCollection<WorkflowWorkItem>>
        QueryAsync(
            string procedure,
            object parameters,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                procedure,
                parameters,
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        var records =
            await connection.QueryAsync<
                WorkflowWorkItemRecord>(
                    command);

        return records
            .Select(record =>
                record.ToDomain())
            .ToArray();
    }
}
