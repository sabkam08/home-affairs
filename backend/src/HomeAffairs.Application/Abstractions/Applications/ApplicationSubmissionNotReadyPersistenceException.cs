namespace HomeAffairs.Application.Abstractions.Applications;

public sealed class
    ApplicationSubmissionNotReadyPersistenceException
    : Exception
{
    public ApplicationSubmissionNotReadyPersistenceException(
        string reason)
        : base(reason)
    {
    }
}
