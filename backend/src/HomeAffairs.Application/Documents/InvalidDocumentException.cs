namespace HomeAffairs.Application.Documents;

public sealed class InvalidDocumentException
    : Exception
{
    public InvalidDocumentException(
        string message)
        : base(message)
    {
    }
}
