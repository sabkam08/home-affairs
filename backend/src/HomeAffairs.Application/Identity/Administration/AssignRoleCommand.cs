using MediatR;

namespace HomeAffairs.Application.Identity.Administration;

public sealed record AssignRoleCommand(
    Guid UserId,
    string RoleCode)
    : IRequest;
