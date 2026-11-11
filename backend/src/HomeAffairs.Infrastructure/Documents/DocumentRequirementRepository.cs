using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Domain.Documents;
using Microsoft.Data.SqlClient;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class DocumentRequirementRepository(
    ISqlConnectionFactory connectionFactory)
    : IDocumentRequirementRepository
{
    public async Task<
        IReadOnlyCollection<DocumentRequirement>>
        GetByApplicationTypeAsync(
            string applicationType,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                commandText:
                    "[documents].DocumentRequirement_GetByApplicationType",
                parameters: new
                {
                    ApplicationType =
                        applicationType
                },
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    cancellationToken);

        var records =
            await connection.QueryAsync<
                DocumentRequirementRecord>(
                    command);

        return records
            .Select(record =>
                record.ToDomain())
            .ToArray();
    }

    public async Task<
        IReadOnlyCollection<
            DocumentRequirementReadiness>>
        GetReadinessForApplicantAsync(
            Guid applicationId,
            Guid userId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                commandText:
                    "[documents].DocumentRequirement_GetReadinessForApplicant",
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

        try
        {
            var records =
                await connection.QueryAsync<
                    DocumentRequirementReadinessRecord>(
                        command);

            return records
                .Select(record =>
                    record.ToDomain())
                .ToArray();
        }
        catch (SqlException exception)
            when (exception.Number == 53301)
        {
            throw new
                DocumentReadinessApplicationNotFoundPersistenceException();
        }
    }
}
