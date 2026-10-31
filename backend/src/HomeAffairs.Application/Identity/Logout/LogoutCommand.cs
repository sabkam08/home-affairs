using MediatR;

namespace HomeAffairs.Application.Identity.Logout;

public sealed record LogoutCommand(
    string RefreshToken)
    : IRequest;
