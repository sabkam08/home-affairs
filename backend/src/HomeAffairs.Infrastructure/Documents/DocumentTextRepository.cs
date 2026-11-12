using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Domain.Documents;
using Microsoft.Data.SqlClient;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class DocumentTextRepository(
    ISqlConnectionFactory connectionFactory)
    : IDocumentTextRepository
{
    public async Task<DocumentTextExtraction>
        SaveForApplicantAsync(
            Guid documentId,
            Guid applicationId,
            Guid userId,
            string extractionStatus,
            string? extractionMethod,
            string? extractedText,
            DateTimeOffset processedAtUtc,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                commandText:
                    "[documents].DocumentText_SaveForApplicant",

                parameters: new
                {
                    DocumentId =
                        documentId,

                    ApplicationId =
                        applicationId,

                    UserId =
                        userId,

                    ExtractionStatus =
                        extractionStatus,

                    ExtractionMethod =
                        extractionMethod,

                    ExtractedText =
                        extractedText,

                    ProcessedAtUtc =
                        processedAtUtc.UtcDateTime
                },

                commandType:
                    CommandType.StoredProcedure,

                cancellationToken:
                    cancellationToken);

        try
        {
            var record =
                await connection.QuerySingleAsync<
                    DocumentTextExtractionRecord>(
                        command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 53401)
        {
            throw new
                DocumentNotFoundPersistenceException();
        }
    }

    public async Task<DocumentTextExtraction?>
        GetForApplicantAsync(
            Guid documentId,
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
                    "[documents].DocumentText_GetForApplicant",

                parameters: new
                {
                    DocumentId =
                        documentId,

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
                    DocumentTextExtractionRecord>(
                        command);

        return record?.ToDomain();
    }

    public async Task<
        IReadOnlyCollection<DocumentSearchResult>>
        SearchForApplicantAsync(
            Guid applicationId,
            Guid userId,
            string searchText,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                commandText:
                    "[documents].DocumentText_SearchForApplicant",

                parameters: new
                {
                    ApplicationId =
                        applicationId,

                    UserId =
                        userId,

                    SearchText =
                        searchText
                },

                commandType:
                    CommandType.StoredProcedure,

                cancellationToken:
                    cancellationToken);

        try
        {
            var records =
                await connection.QueryAsync<
                    DocumentSearchRecord>(
                        command);

            return records
                .Select(record =>
                    record.ToDomain())
                .ToArray();
        }
        catch (SqlException exception)
            when (exception.Number == 53402)
        {
            throw new
                DocumentReadinessApplicationNotFoundPersistenceException();
        }
    }
}
