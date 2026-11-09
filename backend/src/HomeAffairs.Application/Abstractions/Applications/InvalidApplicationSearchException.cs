namespace HomeAffairs.Application.Abstractions.Applications;

public sealed class InvalidApplicationSearchException
    : Exception
{
    public InvalidApplicationSearchException(
        string message)
        : base(message)
    {
    }
}
