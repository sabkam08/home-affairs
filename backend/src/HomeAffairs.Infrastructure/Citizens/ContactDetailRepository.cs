using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using HomeAffairs.Application.Abstractions.Citizens;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Domain.Citizens;

namespace HomeAffairs.Infrastructure.Citizens;

internal sealed class ContactDetailRepository(
    ISqlConnectionFactory connectionFactory)
    : IContactDetailRepository
{
    public async Task<IReadOnlyCollection<ContactDetail>>
        GetByCitizenIdAsync(
            Guid citizenId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].ContactDetail_GetByCitizenId",
            parameters: new
            {
                CitizenId = citizenId
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        var records =
            await connection.QueryAsync<
                ContactDetailRecord>(command);

        return records
            .Select(record => record.ToDomain())
            .ToArray();
    }

    public async Task<ContactDetail> CreateAsync(
        ContactDetail contactDetail,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].ContactDetail_Create",
            parameters: new
            {
                contactDetail.ContactDetailId,
                contactDetail.CitizenId,
                UserId = userId,
                contactDetail.ContactType,
                contactDetail.ContactValue,
                contactDetail.IsPrimary,
                CreatedAtUtc =
                    contactDetail
                        .CreatedAtUtc
                        .UtcDateTime
            },
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        try
        {
            var record =
                await connection.QuerySingleAsync<
                    ContactDetailRecord>(command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52103)
        {
            throw new DuplicateContactDetailException();
        }
    }

    public async Task<ContactDetail?> GetByIdForUserAsync(
    Guid contactDetailId,
    Guid userId,
    CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].ContactDetail_GetByIdForUser",
            parameters: new
            {
                ContactDetailId =
                    contactDetailId,

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
                    ContactDetailRecord>(command);

        return record?.ToDomain();
    }

    public async Task<ContactDetail> UpdateAsync(
    ContactDetail contactDetail,
    Guid userId,
    byte[] expectedRowVersion,
    CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].ContactDetail_Update",
            parameters: new
            {
                contactDetail.ContactDetailId,

                UserId =
                    userId,

                contactDetail.ContactType,
                contactDetail.ContactValue,
                contactDetail.IsPrimary,

                ExpectedRowVersion =
                    expectedRowVersion,

                UpdatedAtUtc =
                    contactDetail
                        .UpdatedAtUtc!
                        .Value
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
                    .QuerySingleAsync<
                        ContactDetailRecord>(command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 52103)
        {
            throw new DuplicateContactDetailException();
        }
        catch (SqlException exception)
            when (exception.Number == 52104)
        {
            throw new ContactDetailNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52105)
        {
            throw new ContactDetailConcurrencyException();
        }
    }

    public async Task DeleteAsync(
    Guid contactDetailId,
    Guid userId,
    byte[] expectedRowVersion,
    DateTimeOffset deletedAtUtc,
    CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[citizens].ContactDetail_Delete",
            parameters: new
            {
                ContactDetailId =
                    contactDetailId,

                UserId =
                    userId,

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
            when (exception.Number == 52104)
        {
            throw new ContactDetailNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 52105)
        {
            throw new ContactDetailConcurrencyException();
        }
    }
}
