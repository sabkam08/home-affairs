using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HomeAffairs.Infrastructure.Identity;

internal sealed class JwtAccessTokenProvider(
    IOptions<JwtOptions> options,
    IClock clock)
    : IAccessTokenProvider
{
    private readonly JwtOptions _options = options.Value;

    public AccessTokenResult Create(
        User user,
        IReadOnlyCollection<string> roles)
    {
        var issuedAtUtc = clock.UtcNow;
        var expiresAtUtc =
            issuedAtUtc.AddMinutes(
                _options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.UserId.ToString()),

            new(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()),

            new(
                ClaimTypes.Email,
                user.Email),

            new(
                ClaimTypes.GivenName,
                user.FirstName),

            new(
                ClaimTypes.Surname,
                user.LastName)
        };

        claims.AddRange(
            roles.Select(
                role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAtUtc.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        var serializedToken =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new AccessTokenResult(
            serializedToken,
            expiresAtUtc);
    }
}
