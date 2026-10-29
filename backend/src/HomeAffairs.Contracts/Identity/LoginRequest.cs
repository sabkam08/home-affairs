namespace HomeAffairs.Contracts.Identity;

public sealed record LoginRequest(
    string Email,
    string Password);
