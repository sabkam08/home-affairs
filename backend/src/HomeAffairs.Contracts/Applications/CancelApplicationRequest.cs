namespace HomeAffairs.Contracts.Applications;

public sealed record CancelApplicationRequest(
    string RowVersion,
    string? Reason);
