namespace HomeAffairs.Application.Abstractions.Applications;

public sealed class InvalidApplicationStatePersistenceException
    : Exception
{
    public InvalidApplicationStatePersistenceException(
        string operation)
    {
        Operation = operation;
    }

    public string Operation { get; }
}
