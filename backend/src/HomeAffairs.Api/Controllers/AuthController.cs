using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Identity.Login;
using HomeAffairs.Application.Identity.Register;
using HomeAffairs.Contracts.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeAffairs.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    ISender sender,
    IUserRepository userRepository)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(
        typeof(AuthenticationResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthenticationResponse>>
        Register(
            RegisterRequest request,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName),
            cancellationToken);

        var response = new AuthenticationResponse(
            result.UserId,
            result.Email,
            result.FirstName,
            result.LastName,
            result.Roles,
            result.AccessToken,
            result.ExpiresAtUtc);

        return Created(
            $"/api/v1/auth/users/{result.UserId}",
            response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(AuthenticationResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>>
        Login(
            LoginRequest request,
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LoginCommand(
                request.Email,
                request.Password),
            cancellationToken);

        return Ok(
            new AuthenticationResponse(
                result.UserId,
                result.Email,
                result.FirstName,
                result.LastName,
                result.Roles,
                result.AccessToken,
                result.ExpiresAtUtc));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(
        typeof(CurrentUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserResponse>>
        GetCurrentUser(
            CancellationToken cancellationToken)
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

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
            new CurrentUserResponse(
                user.UserId,
                user.Email,
                user.FirstName,
                user.LastName,
                roles));
    }
}
