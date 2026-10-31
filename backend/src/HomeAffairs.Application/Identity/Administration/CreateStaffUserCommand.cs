using MediatR;

namespace HomeAffairs.Application.Identity.Administration;

public sealed record CreateStaffUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string RoleCode)
    : IRequest<CreateStaffUserResult>;

public sealed record CreateStaffUserResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles);
