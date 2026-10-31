namespace HomeAffairs.Contracts.Identity;

public sealed record CreateStaffUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string RoleCode);
