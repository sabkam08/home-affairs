namespace HomeAffairs.Application.Identity;

public static class SecurityEventTypes
{
    public const string UserRegistered = "USER_REGISTERED";

    public const string LoginSucceeded = "LOGIN_SUCCEEDED";

    public const string LoginFailed = "LOGIN_FAILED";

    public const string AccountLocked = "ACCOUNT_LOCKED";

    public const string RefreshTokenIssued = "REFRESH_TOKEN_ISSUED";

    public const string RefreshTokenRotated = "REFRESH_TOKEN_ROTATED";

    public const string RefreshTokenRevoked = "REFRESH_TOKEN_REVOKED";

    public const string RefreshTokenReuseDetected = "REFRESH_TOKEN_REUSE_DETECTED";

    public const string UserLoggedOut = "USER_LOGGED_OUT";
}
