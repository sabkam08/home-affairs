using MediatR;

namespace HomeAffairs.Application.Identity.Login;

public sealed record LoginCommand(
    string Email,
    string Password)
    : IRequest<LoginResult>;

public sealed record LoginResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
