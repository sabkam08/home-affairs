namespace HomeAffairs.Application.Documents;

public sealed class DocumentConflictException
    : Exception
{
    public DocumentConflictException()
        : base(
            "The document was modified by another request.")
    {
    }
}
