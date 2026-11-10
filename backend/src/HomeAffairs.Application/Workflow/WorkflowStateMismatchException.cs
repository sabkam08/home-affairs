namespace HomeAffairs.Application.Workflow;

public sealed class WorkflowStateMismatchException
    : Exception
{
    public WorkflowStateMismatchException()
        : base(
            "The application and workflow states are " +
            "not synchronized.")
    {
    }
}
