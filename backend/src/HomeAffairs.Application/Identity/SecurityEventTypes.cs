namespace HomeAffairs.Application.Identity;

public static class SecurityEventTypes
{
    public const string UserRegistered = "USER_REGISTERED";

    public const string LoginSucceeded = "LOGIN_SUCCEEDED";

    public const string LoginFailed = "LOGIN_FAILED";

    public const string AccountLocked = "ACCOUNT_LOCKED";
}
