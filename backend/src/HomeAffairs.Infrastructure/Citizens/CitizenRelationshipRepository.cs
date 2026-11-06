using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Infrastructure.Citizens;

internal sealed class CitizenRelationshipRepository(
    ISqlConnectionFactory connectionFactory)
    : ICitizenRelationshipRepository
{
    public async Task<CitizenRelationship>
        CreateRepresentedMinorAsync(
            Citizen subject,
            Guid relationshipId,
            Guid representativeUserId,
            string relationshipType,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].Citizen_CreateRepresentedMinor",
            parameters: new
            {
                SubjectCitizenId =
                    subject.CitizenId,

                CitizenRelationshipId =
                    relationshipId,

                RepresentativeUserId =
                    representativeUserId,

                subject.SouthAfricanIdNumber,
                subject.FirstName,
                subject.MiddleNames,
                subject.LastName,

                DateOfBirth =
                    subject.DateOfBirth.ToDateTime(
                        TimeOnly.MinValue),

                subject.Sex,
                RelationshipType =
                    relationshipType,

                CreatedAtUtc =
                    subject.CreatedAtUtc.UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var record =
            await connection.QuerySingleAsync<
                CitizenRelationshipRecord>(
                    command);

        return record.ToDomain();
    }

    public async Task<
        IReadOnlyCollection<CitizenRelationship>>
        GetByRepresentativeUserIdAsync(
            Guid representativeUserId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].CitizenRelationship_GetByRepresentativeUserId",
            parameters: new
            {
                RepresentativeUserId =
                    representativeUserId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var records =
            await connection.QueryAsync<
                CitizenRelationshipRecord>(
                    command);

        return records
            .Select(record => record.ToDomain())
            .ToArray();
    }

    public async Task<CitizenRelationship?>
        GetSubjectForRepresentativeAsync(
            Guid subjectCitizenId,
            Guid representativeUserId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].CitizenRelationship_GetSubjectForRepresentative",
            parameters: new
            {
                SubjectCitizenId =
                    subjectCitizenId,

                RepresentativeUserId =
                    representativeUserId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var record =
            await connection
                .QuerySingleOrDefaultAsync<
                    CitizenRelationshipRecord>(
                        command);

        return record?.ToDomain();
    }

    public async Task<CitizenRelationship>
    UpdateRelationshipAsync(
        Guid subjectCitizenId,
        Guid representativeUserId,
        string relationshipType,
        byte[] expectedRowVersion,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].CitizenRelationship_Update",
            parameters: new
            {
                SubjectCitizenId =
                    subjectCitizenId,

                RepresentativeUserId =
                    representativeUserId,

                RelationshipType =
                    relationshipType,

                ExpectedRowVersion =
                    expectedRowVersion,

                UpdatedAtUtc =
                    updatedAtUtc.UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var record =
                await connection.QuerySingleAsync<
                    CitizenRelationshipRecord>(
                        command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52307)
        {
            throw new
                CitizenRelationshipNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52308)
        {
            throw new
                CitizenRelationshipConcurrencyException();
        }
    }

    public async Task DeactivateRelationshipAsync(
    Guid subjectCitizenId,
    Guid representativeUserId,
    byte[] expectedRowVersion,
    DateTimeOffset deactivatedAtUtc,
    CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].CitizenRelationship_Deactivate",
            parameters: new
            {
                SubjectCitizenId =
                    subjectCitizenId,

                RepresentativeUserId =
                    representativeUserId,

                ExpectedRowVersion =
                    expectedRowVersion,

                DeactivatedAtUtc =
                    deactivatedAtUtc.UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            await connection.ExecuteAsync(command);
        }
        catch (SqlException exception)
            when (exception.Number == 52307)
        {
            throw new
                CitizenRelationshipNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52308)
        {
            throw new
                CitizenRelationshipConcurrencyException();
        }
    }
}
