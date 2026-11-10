using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Application.Abstractions.Workflow;
using HomeAffairs.Domain.Applications;
using Microsoft.Data.SqlClient;

namespace HomeAffairs.Infrastructure.Applications;

internal sealed class ApplicationAssignmentRepository(
    ISqlConnectionFactory connectionFactory)
    : IApplicationAssignmentRepository
{
    public async Task<ApplicationAssignment> AssignAsync(
        Guid applicationId,
        Guid assignedToUserId,
        Guid assignedByUserId,
        string? reason,
        byte[] expectedApplicationRowVersion,
        DateTimeOffset assignedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            "[applications].Application_Assign",
            new
            {
                ApplicationId = applicationId,
                AssignedToUserId = assignedToUserId,
                AssignedByUserId = assignedByUserId,
                Reason = reason,
                AssignedAtUtc =
                    assignedAtUtc.UtcDateTime,
                ExpectedApplicationRowVersion =
                    expectedApplicationRowVersion
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var record =
                await connection.QuerySingleAsync<
                    ApplicationAssignmentRecord>(
                        command);

            return record.ToDomain();
        }
        catch (SqlException ex)
            when (ex.Number == 52501)
        {
            throw new
                ApplicationAssignmentNotFoundException();
        }
        catch (SqlException ex)
            when (ex.Number == 52502)
        {
            throw new InvalidAssignmentStateException();
        }
        catch (SqlException ex)
            when (ex.Number == 52503)
        {
            throw new InvalidAssigneeException();
        }
        catch (SqlException ex)
            when (ex.Number == 52504)
        {
            throw new ApplicationConcurrencyException();
        }
        catch (SqlException exception)
            when (exception.Number == 52906)
        {
            throw new WorkflowStateMismatchPersistenceException();
        }
    }

    public async Task<IReadOnlyCollection<ApplicationAssignment>>
        GetHistoryAsync(
            Guid applicationId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            "[applications].Application_GetAssignmentHistory",
            new
            {
                ApplicationId = applicationId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var records =
            await connection.QueryAsync<
                ApplicationAssignmentRecord>(
                    command);

        return records
            .Select(x => x.ToDomain())
            .ToArray();
    }

    public async Task<
        IReadOnlyCollection<HomeAffairsApplication>>
        GetAssignedToMeAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        return await GetApplicationsAsync(
            "[applications].Application_GetAssignedToMe",
            new
            {
                UserId = userId
            },
            cancellationToken);
    }

    public async Task<
        IReadOnlyCollection<HomeAffairsApplication>>
        GetUnassignedAsync(
            CancellationToken cancellationToken)
    {
        return await GetApplicationsAsync(
            "[applications].Application_GetUnassigned",
            null,
            cancellationToken);
    }

    private async Task<
        IReadOnlyCollection<HomeAffairsApplication>>
        GetApplicationsAsync(
            string procedure,
            object? parameters,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            procedure,
            parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var records =
            await connection.QueryAsync<
                ApplicationRecord>(command);

        return records
            .Select(x => x.ToDomain())
            .ToArray();
    }
}
