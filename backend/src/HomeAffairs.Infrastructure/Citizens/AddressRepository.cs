using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Domain.Citizens;
using Microsoft.Data.SqlClient;

namespace HomeAffairs.Infrastructure.Citizens;

internal sealed class AddressRepository(
    ISqlConnectionFactory connectionFactory)
    : IAddressRepository
{
    public async Task<IReadOnlyCollection<CitizenAddress>>
        GetByCitizenIdAsync(
            Guid citizenId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            "[citizens].Address_GetByCitizenId",
            new
            {
                CitizenId = citizenId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var results =
            await connection.QueryAsync<AddressRecord>(
                command);

        return results
            .Select(result => result.ToDomain())
            .ToArray();
    }

    public async Task<CitizenAddress?>
        GetByIdForUserAsync(
            Guid addressId,
            Guid userId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            "[citizens].Address_GetByIdForUser",
            new
            {
                AddressId = addressId,
                UserId = userId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var result =
            await connection
                .QuerySingleOrDefaultAsync<
                    AddressRecord>(command);

        return result?.ToDomain();
    }

    public async Task<CitizenAddress> CreateAsync(
        CitizenAddress address,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            CreateWriteCommand(
                "[citizens].Address_Create",
                address,
                userId,
                expectedRowVersion: null,
                cancellationToken);

        var result =
            await connection
                .QuerySingleAsync<AddressRecord>(
                    command);

        return result.ToDomain();
    }

    public async Task<CitizenAddress> UpdateAsync(
        CitizenAddress address,
        Guid userId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            CreateWriteCommand(
                "[citizens].Address_Update",
                address,
                userId,
                expectedRowVersion,
                cancellationToken);

        try
        {
            var result =
                await connection
                    .QuerySingleAsync<AddressRecord>(
                        command);

            return result.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52203)
        {
            throw new AddressNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52204)
        {
            throw new AddressConcurrencyException();
        }
    }

    public async Task DeleteAsync(
        Guid addressId,
        Guid userId,
        byte[] expectedRowVersion,
        DateTimeOffset deletedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            "[citizens].Address_Delete",
            new
            {
                AddressId = addressId,
                UserId = userId,
                ExpectedRowVersion =
                    expectedRowVersion,
                DeletedAtUtc =
                    deletedAtUtc.UtcDateTime
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
            when (exception.Number == 52203)
        {
            throw new AddressNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52204)
        {
            throw new AddressConcurrencyException();
        }
    }

    private static CommandDefinition
        CreateWriteCommand(
            string procedure,
            CitizenAddress address,
            Guid userId,
            byte[]? expectedRowVersion,
            CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();

        parameters.Add(
            "AddressId",
            address.AddressId);

        if (procedure.EndsWith(
                "Address_Create",
                StringComparison.Ordinal))
        {
            parameters.Add(
                "CitizenId",
                address.CitizenId);
        }

        parameters.Add(
            "UserId",
            userId);

        parameters.Add(
            "AddressType",
            address.AddressType);

        parameters.Add(
            "AddressLine1",
            address.AddressLine1);

        parameters.Add(
            "AddressLine2",
            address.AddressLine2);

        parameters.Add(
            "Suburb",
            address.Suburb);

        parameters.Add(
            "CityOrTown",
            address.CityOrTown);

        parameters.Add(
            "Province",
            address.Province);

        parameters.Add(
            "PostalCode",
            address.PostalCode);

        parameters.Add(
            "CountryCode",
            address.CountryCode);

        parameters.Add(
            "IsPrimary",
            address.IsPrimary);

        if (expectedRowVersion is null)
        {
            parameters.Add(
                "CreatedAtUtc",
                address.CreatedAtUtc.UtcDateTime);
        }
        else
        {
            parameters.Add(
                "ExpectedRowVersion",
                expectedRowVersion);

            parameters.Add(
                "UpdatedAtUtc",
                address.UpdatedAtUtc!
                    .Value
                    .UtcDateTime);
        }

        return new CommandDefinition(
            procedure,
            parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);
    }
}
