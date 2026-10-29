using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Persistence;

namespace HomeAffairs.Infrastructure.Identity;

internal sealed class SecurityEventRepository(
    ISqlConnectionFactory connectionFactory)
    : ISecurityEventRepository
{
    public async Task CreateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText: "identity.SecurityEvent_Create",
            parameters: new
            {
                securityEvent.SecurityEventId,
                securityEvent.UserId,
                securityEvent.EventType,
                securityEvent.Description,
                securityEvent.IpAddress,
                securityEvent.UserAgent,
                securityEvent.CorrelationId,
                OccurredAtUtc =
                    securityEvent.OccurredAtUtc.UtcDateTime
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }
}
