using MediatR;

namespace HomeAffairs.Application.Identity.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName)
    : IRequest<RegisterResult>;

public sealed record RegisterResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
