using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Domain.Identity;

namespace HomeAffairs.Infrastructure.Identity;

internal sealed class UserRepository(
    ISqlConnectionFactory connectionFactory)
    : IUserRepository
{
    public async Task<User?> GetByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText: "identity.User_GetByEmail",
            parameters: new
            {
                NormalizedEmail = normalizedEmail
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<User>(
            command);
    }

    public async Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText: "identity.User_GetById",
            parameters: new
            {
                UserId = userId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<User>(
            command);
    }

    public async Task<IReadOnlyCollection<string>> GetRolesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText: "identity.User_GetRoles",
            parameters: new
            {
                UserId = userId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var roles =
            await connection.QueryAsync<string>(command);

        return roles.ToArray();
    }

    public async Task CreateCitizenAsync(
        User user,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText: "identity.User_CreateCitizen",
            parameters: new
            {
                user.UserId,
                user.Email,
                user.NormalizedEmail,
                user.PasswordHash,
                user.FirstName,
                user.LastName,
                user.CreatedAtUtc
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task<LoginAttemptResult> RecordFailedLoginAsync(
    Guid userId,
    int maximumFailedLoginAttempts,
    int lockoutDurationMinutes,
    DateTimeOffset occurredAtUtc,
    CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText: "identity.User_RecordFailedLogin",
            parameters: new
            {
                UserId = userId,
                MaximumFailedLoginAttempts =
                    maximumFailedLoginAttempts,
                LockoutDurationMinutes =
                    lockoutDurationMinutes,
                OccurredAtUtc = occurredAtUtc.UtcDateTime
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var result =
            await connection.QuerySingleAsync<LoginAttemptRecord>(
                command);

        return new LoginAttemptResult(
            result.FailedLoginAttempts,
            result.LockedUntilUtc.HasValue
                ? new DateTimeOffset(
                    DateTime.SpecifyKind(
                        result.LockedUntilUtc.Value,
                        DateTimeKind.Utc))
                : null);
    }

    public async Task RecordSuccessfulLoginAsync(
        Guid userId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "identity.User_RecordSuccessfulLogin",
            parameters: new
            {
                UserId = userId,
                OccurredAtUtc = occurredAtUtc.UtcDateTime
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    private sealed class LoginAttemptRecord
    {
        public int FailedLoginAttempts { get; init; }

        public DateTime? LockedUntilUtc { get; init; }
    }
}
