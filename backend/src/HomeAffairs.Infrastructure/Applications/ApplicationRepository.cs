using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Applications;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Domain.Applications;
using Microsoft.Data.SqlClient;

namespace HomeAffairs.Infrastructure.Applications;

internal sealed class ApplicationRepository(
    ISqlConnectionFactory connectionFactory)
    : IApplicationRepository
{
    public async Task<HomeAffairsApplication>
        CreateDraftAsync(
            HomeAffairsApplication application,
            Guid userId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_CreateDraft",
            parameters: new
            {
                application.ApplicationId,
                application.ApplicationNumber,
                application.ApplicationType,
                application.ApplicantCitizenId,
                application.SubjectCitizenId,

                UserId =
                    userId,

                CreatedAtUtc =
                    application.CreatedAtUtc.UtcDateTime
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
                        ApplicationRecord>(
                            command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52401)
        {
            throw new
                ApplicationApplicantNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52402)
        {
            throw new
                ApplicationSubjectNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52403)
        {
            throw new
                ApplicationSubjectAccessDeniedPersistenceException();
        }
    }

    public async Task<HomeAffairsApplication?>
        GetByIdForApplicantAsync(
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_GetByIdForApplicant",
            parameters: new
            {
                ApplicationId =
                    applicationId,

                UserId =
                    userId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var record =
            await connection
                .QuerySingleOrDefaultAsync<
                    ApplicationRecord>(
                        command);

        return record?.ToDomain();
    }

    public async Task<
        IReadOnlyCollection<HomeAffairsApplication>>
        GetMineAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[applications].Application_GetMine",
            parameters: new
            {
                UserId =
                    userId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var records =
            await connection.QueryAsync<
                ApplicationRecord>(
                    command);

        return records
            .Select(record => record.ToDomain())
            .ToArray();
    }
}
