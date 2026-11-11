namespace HomeAffairs.Application.Documents;

public sealed class DuplicateDocumentContentException
    : Exception
{
    public DuplicateDocumentContentException()
        : base(
            "The uploaded file is identical to " +
            "the latest document version.")
    {
    }
}
