using System.Security.Cryptography;
using HomeAffairs.Application.Abstractions.Identity;

namespace HomeAffairs.Infrastructure.Identity;

internal sealed class RefreshTokenGenerator
    : IRefreshTokenGenerator
{
    private const int TokenSizeInBytes = 64;

    public GeneratedRefreshToken Generate()
    {
        var randomBytes =
            RandomNumberGenerator.GetBytes(TokenSizeInBytes);

        var token = Convert.ToBase64String(randomBytes);

        return new GeneratedRefreshToken(
            token,
            SHA512.HashData(randomBytes));
    }

    public byte[] ComputeHash(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException(
                "Refresh token is required.",
                nameof(refreshToken));
        }

        byte[] tokenBytes;

        try
        {
            tokenBytes =
                Convert.FromBase64String(refreshToken);
        }
        catch (FormatException)
        {
            /*
                Return a deterministic non-matching hash rather than
                exposing token-format details to the caller.
            */
            tokenBytes =
                System.Text.Encoding.UTF8.GetBytes(
                    refreshToken);
        }

        return SHA512.HashData(tokenBytes);
    }
}
