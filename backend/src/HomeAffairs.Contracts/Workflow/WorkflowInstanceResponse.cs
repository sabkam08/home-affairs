namespace HomeAffairs.Contracts.Workflow;

public sealed record WorkflowInstanceResponse(
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
    string RowVersion);
