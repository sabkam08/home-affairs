namespace HomeAffairs.Domain.Workflow;

public sealed class WorkflowDefinition
{
    public Guid WorkflowDefinitionId { get; init; }

    public string ApplicationType { get; init; } =
        string.Empty;

    public string Name { get; init; } =
        string.Empty;

    public int Version { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = [];

    public IReadOnlyCollection<WorkflowState> States
    { get; init; } = [];

    public IReadOnlyCollection<WorkflowTransition>
        Transitions
    { get; init; } = [];

    public WorkflowState? InitialState =>
        States.SingleOrDefault(
            state => state.IsInitial);

    public bool IsTransitionAllowed(
        string fromState,
        string toState)
    {
        return Transitions.Any(
            transition =>
                transition.IsActive
                &&
                string.Equals(
                    transition.FromStateCode,
                    fromState,
                    StringComparison.OrdinalIgnoreCase)
                &&
                string.Equals(
                    transition.ToStateCode,
                    toState,
                    StringComparison.OrdinalIgnoreCase));
    }

    public WorkflowTransition?
        FindTransition(
            string fromState,
            string actionCode)
    {
        return Transitions.SingleOrDefault(
            transition =>
                transition.IsActive
                &&
                string.Equals(
                    transition.FromStateCode,
                    fromState,
                    StringComparison.OrdinalIgnoreCase)
                &&
                string.Equals(
                    transition.ActionCode,
                    actionCode,
                    StringComparison.OrdinalIgnoreCase));
    }
}
