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
}
