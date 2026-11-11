namespace HomeAffairs.Application.Documents;

public sealed class DocumentNotFoundException
    : Exception
{
    public DocumentNotFoundException()
        : base(
            "The document could not be found.")
    {
    }
}
