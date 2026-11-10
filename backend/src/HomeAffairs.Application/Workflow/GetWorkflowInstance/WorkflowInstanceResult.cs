namespace HomeAffairs.Application.Workflow.GetWorkflowInstance;

public sealed record WorkflowInstanceResult(
    Guid WorkflowInstanceId,
    Guid WorkflowDefinitionId,
    Guid ApplicationId,
    string ApplicationType,
    string WorkflowName,
    int WorkflowVersion,
    string CurrentStateCode,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    byte[] RowVersion);
