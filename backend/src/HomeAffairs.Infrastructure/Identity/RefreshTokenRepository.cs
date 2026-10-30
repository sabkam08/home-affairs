using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Persistence;

namespace HomeAffairs.Infrastructure.Identity;

internal sealed class RefreshTokenRepository(
    ISqlConnectionFactory connectionFactory)
    : IRefreshTokenRepository
{
    public async Task CreateAsync(
        RefreshTokenRecord refreshToken,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[identity].RefreshToken_Create",
            parameters: new
            {
                refreshToken.RefreshTokenId,
                refreshToken.UserId,
                refreshToken.TokenHash,
                CreatedAtUtc =
                    refreshToken.CreatedAtUtc.UtcDateTime,
                ExpiresAtUtc =
                    refreshToken.ExpiresAtUtc.UtcDateTime,
                refreshToken.CreatedByIpAddress
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task<RefreshTokenRotationResult>
        RotateAsync(
            byte[] currentTokenHash,
            RefreshTokenRecord replacementToken,
            string? ipAddress,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[identity].RefreshToken_Rotate",
            parameters: new
            {
                CurrentTokenHash = currentTokenHash,
                NewRefreshTokenId =
                    replacementToken.RefreshTokenId,
                NewTokenHash =
                    replacementToken.TokenHash,
                CreatedAtUtc =
                    replacementToken.CreatedAtUtc.UtcDateTime,
                ExpiresAtUtc =
                    replacementToken.ExpiresAtUtc.UtcDateTime,
                IpAddress = ipAddress
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result =
            await connection
                .QuerySingleAsync<RotationRecord>(command);

        return new RefreshTokenRotationResult(
            result.RotationStatus,
            result.UserId);
    }

    public async Task<RefreshTokenRevocationResult>
        RevokeAsync(
            byte[] tokenHash,
            DateTimeOffset revokedAtUtc,
            string? ipAddress,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[identity].RefreshToken_Revoke",
            parameters: new
            {
                TokenHash = tokenHash,
                RevokedAtUtc = revokedAtUtc.UtcDateTime,
                RevokedByIpAddress = ipAddress
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result =
            await connection
                .QuerySingleAsync<RevocationRecord>(command);

        return new RefreshTokenRevocationResult(
            result.TokenFound,
            result.UserId);
    }

    public async Task<int> RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset revokedAtUtc,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[identity].RefreshToken_RevokeAllForUser",
            parameters: new
            {
                UserId = userId,
                RevokedAtUtc = revokedAtUtc.UtcDateTime,
                RevokedByIpAddress = ipAddress
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<int>(
            command);
    }

    private sealed class RotationRecord
    {
        public string RotationStatus { get; init; } =
            string.Empty;

        public Guid? UserId { get; init; }
    }

    private sealed class RevocationRecord
    {
        public bool TokenFound { get; init; }

        public Guid? UserId { get; init; }
    }
}
