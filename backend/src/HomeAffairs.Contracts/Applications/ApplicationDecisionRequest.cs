namespace HomeAffairs.Contracts.Applications;

public sealed record ApplicationDecisionRequest(
    string RowVersion,
    string? Reason);
