using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Route("api/v1/authorization-test")]
public sealed class AuthorizationTestController(
    ICurrentUser currentUser)
    : ControllerBase
{
    [Authorize(
        Policy =
            AuthorizationPolicies.ApplicationCreate)]
    [HttpGet("citizen")]
    public IActionResult CitizenPermission()
    {
        return Ok(new
        {
            message =
                "APPLICATION.CREATE permission granted.",
            userId = currentUser.UserId
        });
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ApplicationProcess)]
    [HttpGet("practitioner")]
    public IActionResult PractitionerPermission()
    {
        return Ok(new
        {
            message =
                "APPLICATION.PROCESS permission granted.",
            userId = currentUser.UserId
        });
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ApplicationApprove)]
    [HttpGet("supervisor")]
    public IActionResult SupervisorPermission()
    {
        return Ok(new
        {
            message =
                "APPLICATION.APPROVE permission granted.",
            userId = currentUser.UserId
        });
    }

    [Authorize(
        Policy =
            AuthorizationPolicies.ManageUsers)]
    [HttpGet("administration")]
    public IActionResult AdministrationPermission()
    {
        return Ok(new
        {
            message =
                "IDENTITY.USER_MANAGE permission granted.",
            userId = currentUser.UserId
        });
    }
}
