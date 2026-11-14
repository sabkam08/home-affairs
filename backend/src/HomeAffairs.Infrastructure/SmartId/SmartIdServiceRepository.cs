using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Application.Abstractions.SmartId;
using HomeAffairs.Domain.SmartId;
using Microsoft.Data.SqlClient;

namespace HomeAffairs.Infrastructure.SmartId;

internal sealed class SmartIdServiceRepository(
    ISqlConnectionFactory connectionFactory)
    : ISmartIdServiceRepository
{
    public async Task<SmartIdServiceDetails>
        SaveForApplicantAsync(
            SmartIdServiceDetails details,
            Guid userId,
            byte[]? expectedRowVersion,
            DateTimeOffset changedAtUtc,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command =
            new CommandDefinition(
                commandText:
                    "[smartid].SmartIdServiceDetails_SaveForApplicant",

                parameters: new
                {
                    details.SmartIdServiceDetailsId,

                    details.ApplicationId,

                    UserId =
                        userId,

                    details.ExistingSmartIdNumber,

                    details.RequestedFirstName,

                    details.RequestedMiddleNames,

                    details.RequestedLastName,

                    RequestedDateOfBirth =
                        details.RequestedDateOfBirth?
                            .ToDateTime(
                                TimeOnly.MinValue),

                    details.RequestedSex,

                    details.ReplacementReason,

                    IncidentDate =
                        details.IncidentDate?
                            .ToDateTime(
                                TimeOnly.MinValue),

                    details.PoliceCaseNumber,

                    details.PoliceStation,

                    details.DamageDescription,

                    details.CorrectionField,

                    details.CurrentValue,

                    details.RequestedValue,

                    details.RequestReason,

                    ExpectedRowVersion =
                        expectedRowVersion,

                    ChangedAtUtc =
                        changedAtUtc.UtcDateTime
                },

                commandType:
                    CommandType.StoredProcedure,

                cancellationToken:
                    cancellationToken);

        try
        {
            var record =
                await connection.QuerySingleAsync<
                    SmartIdServiceDetailsRecord>(
                        command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 53701)
        {
            throw new
                SmartIdApplicationNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 53702)
        {
            throw new
                InvalidSmartIdApplicationTypePersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 53703)
        {
            throw new
                SmartIdDetailsNotEditablePersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number is 53704 or 53705)
        {
            throw new
                SmartIdDetailsConcurrencyPersistenceException();
        }
    }

    public async Task<SmartIdServiceDetails?>
        GetForApplicantAsync(
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
                    "[smartid].SmartIdServiceDetails_GetForApplicant",

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
                    SmartIdServiceDetailsRecord>(
                        command);

        return record?.ToDomain();
    }
}
