using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Identity.Administration;
using HomeAffairs.Contracts.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Route("api/v1/identity/users")]
public sealed class IdentityAdministrationController(
    ISender sender,
    IUserRepository userRepository)
    : ControllerBase
{
    [Authorize(
        Policy = AuthorizationPolicies.ManageUsers)]
    [HttpPost("staff")]
    public async Task<
        ActionResult<CreateStaffUserResult>>
        CreateStaffUser(
            CreateStaffUserRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await sender.Send(
                new CreateStaffUserCommand(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    request.RoleCode),
                cancellationToken);

        return Created(
            $"/api/v1/identity/users/{result.UserId}",
            result);
    }

    [Authorize(
        Policy = AuthorizationPolicies.ManageRoles)]
    [HttpPut("{userId:guid}/roles/{roleCode}")]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        string roleCode,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new AssignRoleCommand(
                userId,
                roleCode),
            cancellationToken);

        return NoContent();
    }

    [Authorize(
        Policy = AuthorizationPolicies.ManageRoles)]
    [HttpDelete("{userId:guid}/roles/{roleCode}")]
    public async Task<IActionResult> RemoveRole(
        Guid userId,
        string roleCode,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new RemoveRoleCommand(
                userId,
                roleCode),
            cancellationToken);

        return NoContent();
    }

    [Authorize(
        Policy = AuthorizationPolicies.ManageUsers)]
    [HttpGet("{userId:guid}/roles")]
    public async Task<ActionResult<UserRolesResponse>>
        GetRoles(
            Guid userId,
            CancellationToken cancellationToken)
    {
        var user =
            await userRepository.GetByIdAsync(
                userId,
                cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        var roles =
            await userRepository.GetRolesAsync(
                userId,
                cancellationToken);

        return Ok(
            new UserRolesResponse(
                userId,
                roles));
    }
}
