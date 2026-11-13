namespace HomeAffairs.Application.Applications;

public sealed class ApplicationSubmissionNotReadyException
    : Exception
{
    public ApplicationSubmissionNotReadyException(
        string reason)
        : base(reason)
    {
    }
}
