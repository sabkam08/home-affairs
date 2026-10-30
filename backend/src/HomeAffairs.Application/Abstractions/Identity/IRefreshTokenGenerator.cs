namespace HomeAffairs.Application.Abstractions.Identity;

public interface IRefreshTokenGenerator
{
    GeneratedRefreshToken Generate();

    byte[] ComputeHash(string refreshToken);
}

public sealed record GeneratedRefreshToken(
    string Token,
    byte[] TokenHash);
