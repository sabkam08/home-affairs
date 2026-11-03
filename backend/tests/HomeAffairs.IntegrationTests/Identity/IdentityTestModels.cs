namespace HomeAffairs.IntegrationTests.Identity;

internal sealed record TestAuthenticationResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

internal sealed record TestPermissionsResponse(
    Guid UserId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

internal sealed record TestCurrentUserResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles);
