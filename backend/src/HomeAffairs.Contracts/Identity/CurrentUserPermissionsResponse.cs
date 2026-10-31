namespace HomeAffairs.Contracts.Identity;

public sealed record CurrentUserPermissionsResponse(
    Guid UserId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
