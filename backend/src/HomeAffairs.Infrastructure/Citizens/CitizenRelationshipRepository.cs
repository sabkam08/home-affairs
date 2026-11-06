using System.Data;
using Dapper;
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
}
