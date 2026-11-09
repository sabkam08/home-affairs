namespace HomeAffairs.Contracts.Applications;

public sealed record ProcessApplicationRequest(
    string RowVersion,
    string? Reason);
