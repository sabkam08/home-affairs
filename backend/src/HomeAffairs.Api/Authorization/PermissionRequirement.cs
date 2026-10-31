using Microsoft.AspNetCore.Authorization;

namespace HomeAffairs.Api.Authorization;

public sealed class PermissionRequirement(
    string permission)
    : IAuthorizationRequirement
{
    public string Permission { get; } =
        permission;
}
