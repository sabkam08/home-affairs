using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Application.Abstractions.Workflow;
using HomeAffairs.Domain.Workflow;
using Microsoft.Data.SqlClient;

namespace HomeAffairs.Infrastructure.Workflow;

internal sealed class WorkflowInstanceRepository(
    ISqlConnectionFactory connectionFactory)
    : IWorkflowInstanceRepository
{
    public async Task<WorkflowInstance?>
        GetByApplicationIdAsync(
            Guid applicationId,
            CancellationToken cancellationToken)
    {
        return await GetSingleAsync(
            "[workflow].WorkflowInstance_GetByApplicationId",
            new
            {
                ApplicationId = applicationId
            },
            cancellationToken);
    }

    public async Task<WorkflowInstance?>
        GetForApplicantAsync(
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken)
    {
        return await GetSingleAsync(
            "[workflow].WorkflowInstance_GetForApplicant",
            new
            {
                ApplicationId = applicationId,
                UserId = userId
            },
            cancellationToken);
    }

    public async Task<
        IReadOnlyCollection<WorkflowTransition>>
        GetAvailableTransitionsAsync(
            Guid applicationId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                commandText:
                    "[workflow].WorkflowInstance_GetAvailableTransitions",
                parameters: new
                {
                    ApplicationId =
                        applicationId
                },
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        var records =
            await connection.QueryAsync<
                WorkflowTransitionRecord>(
                    command);

        return records
            .Select(record =>
                new WorkflowTransition
                {
                    WorkflowTransitionId =
                        record.WorkflowTransitionId,

                    WorkflowDefinitionId =
                        record.WorkflowDefinitionId,

                    FromStateCode =
                        record.FromStateCode,

                    ToStateCode =
                        record.ToStateCode,

                    ActionCode =
                        record.ActionCode,

                    DisplayName =
                        record.DisplayName,

                    RequiredPermission =
                        record.RequiredPermission,

                    RequiresReason =
                        record.RequiresReason,

                    IsActive =
                        record.IsActive
                })
            .ToArray();
    }

    public async Task<WorkflowInstance>
        SynchronizeStateAsync(
            Guid applicationId,
            string expectedCurrentState,
            string targetState,
            byte[] expectedRowVersion,
            DateTimeOffset changedAtUtc,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                commandText:
                    "[workflow].WorkflowInstance_SynchronizeState",
                parameters: new
                {
                    ApplicationId =
                        applicationId,

                    ExpectedCurrentStateCode =
                        expectedCurrentState,

                    TargetStateCode =
                        targetState,

                    ExpectedRowVersion =
                        expectedRowVersion,

                    ChangedAtUtc =
                        changedAtUtc.UtcDateTime
                },
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        try
        {
            var record =
                await connection
                    .QuerySingleAsync<
                        WorkflowInstanceRecord>(
                            command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52901)
        {
            throw new
                WorkflowInstanceNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52902)
        {
            throw new
                WorkflowTransitionNotAllowedPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52903)
        {
            throw new
                WorkflowInstanceConcurrencyException();
        }
    }

    private async Task<WorkflowInstance?> GetSingleAsync(
        string procedure,
        object parameters,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                commandText:
                    procedure,
                parameters:
                    parameters,
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        var record =
            await connection
                .QuerySingleOrDefaultAsync<
                    WorkflowInstanceRecord>(
                        command);

        return record?.ToDomain();
    }
}
