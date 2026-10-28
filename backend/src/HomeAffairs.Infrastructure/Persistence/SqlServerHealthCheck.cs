using Dapper;
using HomeAffairs.Application.Abstractions.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HomeAffairs.Infrastructure.Persistence;

internal sealed class SqlServerHealthCheck(
    ISqlConnectionFactory connectionFactory)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection =
                await connectionFactory.OpenConnectionAsync(
                    cancellationToken);

            var command = new CommandDefinition(
                commandText: "SELECT 1;",
                cancellationToken: cancellationToken);

            var result =
                await connection.ExecuteScalarAsync<int>(command);

            return result == 1
                ? HealthCheckResult.Healthy(
                    "SQL Server is reachable.")
                : HealthCheckResult.Degraded(
                    "SQL Server returned an unexpected result.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "SQL Server is unavailable.",
                exception);
        }
    }
}
