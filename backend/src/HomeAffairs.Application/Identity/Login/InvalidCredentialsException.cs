namespace HomeAffairs.Application.Identity.Login;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("The supplied email address or password is incorrect.")
    {
    }
}
