using System.Security.Claims;
using HomeAffairs.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Authorization;

namespace HomeAffairs.Api.Authorization;

internal sealed class PermissionAuthorizationHandler(
    IUserPermissionRepository permissionRepository)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated
            != true)
        {
            return;
        }

        var userIdValue =
            context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            return;
        }

        var hasPermission =
            await permissionRepository
                .HasPermissionAsync(
                    userId,
                    requirement.Permission,
                    CancellationToken.None);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
