namespace HomeAffairs.Application.Applications;

public sealed class InvalidApplicationStateException
    : Exception
{
    public InvalidApplicationStateException(
        string message)
        : base(message)
    {
    }
}
