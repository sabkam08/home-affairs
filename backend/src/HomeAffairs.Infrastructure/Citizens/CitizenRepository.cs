using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Infrastructure.Citizens;

internal sealed class CitizenRepository(
    ISqlConnectionFactory connectionFactory)
    : ICitizenRepository
{
    public async Task<Citizen?> GetByIdAsync(
        Guid citizenId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].Citizen_GetById",
            parameters: new
            {
                CitizenId = citizenId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var record =
            await connection
                .QuerySingleOrDefaultAsync<
                    CitizenRecord>(command);

        return record?.ToDomain();
    }

    public async Task<Citizen?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].Citizen_GetByUserId",
            parameters: new
            {
                UserId = userId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var record =
            await connection
                .QuerySingleOrDefaultAsync<
                    CitizenRecord>(command);

        return record?.ToDomain();
    }

    public async Task<Citizen> CreateForUserAsync(
        Citizen citizen,
        CancellationToken cancellationToken)
    {
        if (!citizen.UserId.HasValue)
        {
            throw new InvalidOperationException(
                "A user ID is required when creating " +
                "a self-service citizen profile.");
        }

        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].Citizen_CreateForUser",
            parameters: new
            {
                citizen.CitizenId,
                UserId =
                    citizen.UserId.Value,
                citizen.SouthAfricanIdNumber,
                citizen.FirstName,
                citizen.MiddleNames,
                citizen.LastName,
                DateOfBirth =
                    citizen.DateOfBirth.ToDateTime(
                        TimeOnly.MinValue),
                citizen.Sex,
                CreatedAtUtc =
                    citizen.CreatedAtUtc.UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var record =
            await connection
                .QuerySingleAsync<CitizenRecord>(
                    command);

        return record.ToDomain();
    }

    public async Task<Citizen> UpdateOwnProfileAsync(
        Citizen citizen,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken)
    {
        if (!citizen.UserId.HasValue)
        {
            throw new InvalidOperationException(
                "A user ID is required when updating " +
                "a self-service citizen profile.");
        }

        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].Citizen_UpdateOwnProfile",
            parameters: new
            {
                citizen.CitizenId,
                UserId =
                    citizen.UserId.Value,
                citizen.SouthAfricanIdNumber,
                citizen.FirstName,
                citizen.MiddleNames,
                citizen.LastName,
                DateOfBirth =
                    citizen.DateOfBirth.ToDateTime(
                        TimeOnly.MinValue),
                citizen.Sex,
                ExpectedRowVersion =
                    expectedRowVersion,
                UpdatedAtUtc =
                    citizen.UpdatedAtUtc?
                        .UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var record =
                await connection
                    .QuerySingleAsync<CitizenRecord>(
                        command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52005)
        {
            throw new CitizenConcurrencyException();
        }
    }
}
