namespace HomeAffairs.Contracts.Identity;

public sealed record AuthenticationResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
