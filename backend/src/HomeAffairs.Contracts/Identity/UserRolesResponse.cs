namespace HomeAffairs.Contracts.Identity;

public sealed record UserRolesResponse(
	Guid UserId,
	IReadOnlyCollection<string> Roles);
