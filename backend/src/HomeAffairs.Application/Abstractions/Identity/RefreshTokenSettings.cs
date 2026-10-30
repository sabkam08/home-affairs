namespace HomeAffairs.Application.Abstractions.Identity;

public sealed class RefreshTokenSettings
{
    public const string SectionName = "Jwt";

    public int RefreshTokenLifetimeDays { get; init; } = 7;
}
