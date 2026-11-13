using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Birth;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Domain.Birth;
using Microsoft.Data.SqlClient;

namespace HomeAffairs.Infrastructure.Birth;

internal sealed class BirthServiceRepository(
    ISqlConnectionFactory connectionFactory)
    : IBirthServiceRepository
{
    public async Task<BirthServiceDetails>
        SaveForApplicantAsync(
            BirthServiceDetails details,
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
                    "[birth].BirthServiceDetails_SaveForApplicant",

                parameters: new
                {
                    details.BirthServiceDetailsId,

                    details.ApplicationId,

                    UserId =
                        userId,

                    details.ChildFirstName,
                    details.ChildMiddleNames,
                    details.ChildLastName,

                    ChildDateOfBirth =
                        details.ChildDateOfBirth?
                            .ToDateTime(
                                TimeOnly.MinValue),

                    details.ChildSex,
                    details.PlaceOfBirth,
                    details.ProvinceOfBirth,
                    details.CountryOfBirth,
                    details.MotherFirstName,
                    details.MotherLastName,
                    details.MotherIdNumber,
                    details.FatherFirstName,
                    details.FatherLastName,
                    details.FatherIdNumber,
                    details.CurrentCertificateNumber,
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
                    BirthServiceDetailsRecord>(
                        command);

            return record.ToDomain();
        }
        catch (SqlException exception)
            when (exception.Number == 53501)
        {
            throw new
                BirthApplicationNotFoundPersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 53502)
        {
            throw new
                InvalidBirthApplicationTypePersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number == 53503)
        {
            throw new
                BirthDetailsNotEditablePersistenceException();
        }
        catch (SqlException exception)
            when (exception.Number is 53504 or 53505)
        {
            throw new
                BirthDetailsConcurrencyPersistenceException();
        }
    }

    public async Task<BirthServiceDetails?>
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
                    "[birth].BirthServiceDetails_GetForApplicant",

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
                    BirthServiceDetailsRecord>(
                        command);

        return record?.ToDomain();
    }
}
