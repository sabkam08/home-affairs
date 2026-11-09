namespace HomeAffairs.Application.Applications.Approval;

public sealed class ApplicationDecisionNotAllowedException
    : Exception
{
    public ApplicationDecisionNotAllowedException(
        string message)
        : base(message)
    {
    }
}
