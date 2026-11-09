namespace HomeAffairs.Application.Applications.Approval;

public sealed class ApplicationSelfApprovalException
    : Exception
{
    public ApplicationSelfApprovalException()
        : base(
            "The assigned practitioner may not decide " +
            "their own application.")
    {
    }
}
