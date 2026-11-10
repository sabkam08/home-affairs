using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Application.Abstractions.Workflow;
using HomeAffairs.Domain.Workflow;

namespace HomeAffairs.Infrastructure.Workflow;

internal sealed class WorkflowDefinitionRepository(
    ISqlConnectionFactory connectionFactory)
    : IWorkflowDefinitionRepository
{
    public async Task<WorkflowDefinition?>
        GetActiveByApplicationTypeAsync(
            string applicationType,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var definitionCommand =
            new CommandDefinition(
                commandText:
                    "[workflow].WorkflowDefinition_GetActiveByApplicationType",
                parameters: new
                {
                    ApplicationType =
                        applicationType
                },
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        var definition =
            await connection
                .QuerySingleOrDefaultAsync<
                    WorkflowDefinitionRecord>(
                        definitionCommand);

        if (definition is null)
        {
            return null;
        }

        var states =
            await GetStatesAsync(
                connection,
                definition.WorkflowDefinitionId,
                cancellationToken);

        var transitions =
            await GetTransitionsAsync(
                connection,
                definition.WorkflowDefinitionId,
                cancellationToken);

        return Map(
            definition,
            states,
            transitions);
    }

    public async Task<bool> IsTransitionAllowedAsync(
        string applicationType,
        string fromState,
        string toState,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                commandText:
                    "[workflow].WorkflowTransition_IsAllowed",
                parameters: new
                {
                    ApplicationType =
                        applicationType,

                    FromStateCode =
                        fromState,

                    ToStateCode =
                        toState
                },
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        return await connection
            .QuerySingleAsync<bool>(
                command);
    }

    private static async Task<
        IReadOnlyCollection<WorkflowStateRecord>>
        GetStatesAsync(
            System.Data.Common.DbConnection connection,
            Guid workflowDefinitionId,
            CancellationToken cancellationToken)
    {
        var command =
            new CommandDefinition(
                commandText:
                    "[workflow].WorkflowState_GetByDefinition",
                parameters: new
                {
                    WorkflowDefinitionId =
                        workflowDefinitionId
                },
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        var records =
            await connection.QueryAsync<
                WorkflowStateRecord>(
                    command);

        return records.ToArray();
    }

    private static async Task<
        IReadOnlyCollection<WorkflowTransitionRecord>>
        GetTransitionsAsync(
            System.Data.Common.DbConnection connection,
            Guid workflowDefinitionId,
            CancellationToken cancellationToken)
    {
        var command =
            new CommandDefinition(
                commandText:
                    "[workflow].WorkflowTransition_GetByDefinition",
                parameters: new
                {
                    WorkflowDefinitionId =
                        workflowDefinitionId
                },
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        var records =
            await connection.QueryAsync<
                WorkflowTransitionRecord>(
                    command);

        return records.ToArray();
    }

    private static WorkflowDefinition Map(
        WorkflowDefinitionRecord definition,
        IReadOnlyCollection<WorkflowStateRecord> states,
        IReadOnlyCollection<WorkflowTransitionRecord>
            transitions)
    {
        return new WorkflowDefinition
        {
            WorkflowDefinitionId =
                definition.WorkflowDefinitionId,

            ApplicationType =
                definition.ApplicationType,

            Name =
                definition.Name,

            Version =
                definition.Version,

            IsActive =
                definition.IsActive,

            CreatedAtUtc =
                ToUtc(
                    definition.CreatedAtUtc),

            UpdatedAtUtc =
                definition.UpdatedAtUtc.HasValue
                    ? ToUtc(
                        definition.UpdatedAtUtc.Value)
                    : null,

            RowVersion =
                definition.RowVersion,

            States =
                states
                    .Select(state =>
                        new WorkflowState
                        {
                            WorkflowStateId =
                                state.WorkflowStateId,

                            WorkflowDefinitionId =
                                state.WorkflowDefinitionId,

                            StateCode =
                                state.StateCode,

                            DisplayName =
                                state.DisplayName,

                            Sequence =
                                state.Sequence,

                            IsInitial =
                                state.IsInitial,

                            IsTerminal =
                                state.IsTerminal
                        })
                    .ToArray(),

            Transitions =
                transitions
                    .Select(transition =>
                        new WorkflowTransition
                        {
                            WorkflowTransitionId =
                                transition
                                    .WorkflowTransitionId,

                            WorkflowDefinitionId =
                                transition
                                    .WorkflowDefinitionId,

                            FromStateCode =
                                transition.FromStateCode,

                            ToStateCode =
                                transition.ToStateCode,

                            ActionCode =
                                transition.ActionCode,

                            DisplayName =
                                transition.DisplayName,

                            RequiredPermission =
                                transition
                                    .RequiredPermission,

                            RequiresReason =
                                transition.RequiresReason,

                            IsActive =
                                transition.IsActive
                        })
                    .ToArray()
        };
    }

    private static DateTimeOffset ToUtc(
        DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc));
    }
}
