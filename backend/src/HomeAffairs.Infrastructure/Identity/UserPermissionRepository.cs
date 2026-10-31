using System.Data;
using Dapper;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Persistence;

namespace HomeAffairs.Infrastructure.Identity;

internal sealed class UserPermissionRepository(
    ISqlConnectionFactory connectionFactory)
    : IUserPermissionRepository
{
    public async Task<IReadOnlyCollection<string>>
        GetPermissionsAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[identity].User_GetPermissions",
            parameters: new
            {
                UserId = userId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var permissions =
            await connection.QueryAsync<string>(
                command);

        return permissions.ToArray();
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string permission,
        CancellationToken cancellationToken)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(
                cancellationToken);

        var command = new CommandDefinition(
            commandText:
                "[identity].User_HasPermission",
            parameters: new
            {
                UserId = userId,
                PermissionCode = permission
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection
            .QuerySingleAsync<bool>(command);
    }
}
