namespace HomeAffairs.Application.Applications;

public sealed class ApplicationConflictException
    : Exception
{
    public ApplicationConflictException()
        : base(
            "The application was modified by another request.")
    {
    }
}
