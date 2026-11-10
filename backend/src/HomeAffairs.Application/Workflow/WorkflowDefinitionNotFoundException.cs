namespace HomeAffairs.Application.Workflow;

public sealed class WorkflowDefinitionNotFoundException
    : Exception
{
    public WorkflowDefinitionNotFoundException()
        : base(
            "The workflow definition could not be found.")
    {
    }
}
