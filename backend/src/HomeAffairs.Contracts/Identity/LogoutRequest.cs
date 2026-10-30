namespace HomeAffairs.Contracts.Identity;

public sealed record LogoutRequest(
    string RefreshToken);
