namespace HomeAffairs.Application.Identity.Refresh;

public sealed class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException()
        : base("The refresh token is invalid or has expired.")
    {
    }
}
