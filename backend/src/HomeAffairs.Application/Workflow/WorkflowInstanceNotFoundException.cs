namespace HomeAffairs.Application.Workflow;

public sealed class WorkflowInstanceNotFoundException
    : Exception
{
    public WorkflowInstanceNotFoundException()
        : base(
            "The workflow instance could not be found.")
    {
    }
}
