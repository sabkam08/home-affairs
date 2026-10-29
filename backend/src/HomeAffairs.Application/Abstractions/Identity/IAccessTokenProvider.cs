using HomeAffairs.Domain.Identity;

namespace HomeAffairs.Application.Abstractions.Identity;

public interface IAccessTokenProvider
{
    AccessTokenResult Create(
        User user,
        IReadOnlyCollection<string> roles);
}

public sealed record AccessTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
