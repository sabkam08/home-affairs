namespace HomeAffairs.Application.Abstractions.Identity;

public interface IUserPermissionRepository
{
    Task<IReadOnlyCollection<string>>
        GetPermissionsAsync(
            Guid userId,
            CancellationToken cancellationToken);

    Task<bool> HasPermissionAsync(
        Guid userId,
        string permission,
        CancellationToken cancellationToken);
}
