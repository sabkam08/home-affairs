using MediatR;

namespace HomeAffairs.Application.Identity.Administration;

public sealed record RemoveRoleCommand(
    Guid UserId,
    string RoleCode)
    : IRequest;
