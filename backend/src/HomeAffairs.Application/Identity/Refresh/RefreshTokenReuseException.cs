namespace HomeAffairs.Application.Identity.Refresh;

public sealed class RefreshTokenReuseException : Exception
{
    public RefreshTokenReuseException()
        : base(
            "Refresh token reuse was detected. " +
            "Authentication is required again.")
    {
    }
}
