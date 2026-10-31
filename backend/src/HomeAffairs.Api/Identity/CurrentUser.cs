using System.Security.Claims;
using HomeAffairs.Application.Abstractions.Identity;

namespace HomeAffairs.Api.Identity;

internal sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    private ClaimsPrincipal? Principal =>
        httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated
        ?? false;

    public Guid? UserId
    {
        get
        {
            var value =
                Principal?.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            return Guid.TryParse(
                value,
                out var userId)
                ? userId
                : null;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(
            ClaimTypes.Email);

    public IReadOnlyCollection<string> Roles =>
        Principal?
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray()
        ?? [];
}
