using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Documents;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Domain.Documents;
using Microsoft.Data.SqlClient;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class DocumentRepository(
    ISqlConnectionFactory connectionFactory)
    : IDocumentRepository
{
    public async Task<Document>
        CreateForApplicantAsync(
            Document document,
            byte[] content,
            Guid userId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                commandText:
                    "[documents].Document_CreateForApplicant",
                parameters: new
                {
                    document.DocumentId,
                    document.ApplicationId,

                    UserId =
                        userId,

                    document.DocumentType,
                    document.FileName,
                    document.ContentType,
                    document.FileExtension,
                    document.FileSizeBytes,
                    document.ContentHash,

                    Content =
                        content,

                    CreatedAtUtc =
                        document.CreatedAtUtc.UtcDateTime
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
                        DocumentRecord>(
                            command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 53101)
        {
            throw new
                DocumentApplicationNotFoundPersistenceException();
        }
    }

    public async Task<IReadOnlyCollection<Document>>
        GetByApplicationForApplicantAsync(
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
                    "[documents].Document_GetByApplicationForApplicant",
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
                    DocumentRecord>(
                        command);

            return records
                .Select(record =>
                    record.ToDomain())
                .ToArray();
        }
        catch (SqlException exception)
            when (exception.Number == 53101)
        {
            throw new
                DocumentApplicationNotFoundPersistenceException();
        }
    }

    public async Task<Document?>
        GetByIdForApplicantAsync(
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
                    "[documents].Document_GetByIdForApplicant",
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
                    DocumentRecord>(
                        command);

        return record?.ToDomain();
    }

    public async Task<DocumentContent?>
        GetContentForApplicantAsync(
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
                    "[documents].Document_GetContentForApplicant",
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
                    DocumentContentRecord>(
                        command);

        return record?.ToDomain();
    }
}
