using MediatR;

namespace HomeAffairs.Application.Identity.Refresh;

public sealed record RefreshTokenCommand(
    string RefreshToken)
    : IRequest<RefreshTokenResult>;

public sealed record RefreshTokenResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
