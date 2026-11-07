using HomeAffairs.Application.Abstractions.Applications;

namespace HomeAffairs.Infrastructure.Applications;

internal sealed class ApplicationNumberGenerator
    : IApplicationNumberGenerator
{
    public string Generate(
        DateTimeOffset createdAtUtc)
    {
        /*
            Example:

            HA-20261106-A1B2C3D4E5F6

            Final uniqueness is protected by the
            database unique constraint.
        */
        var suffix =
            Guid.NewGuid()
                .ToString("N")[..12]
                .ToUpperInvariant();

        return
            $"HA-" +
            $"{createdAtUtc:yyyyMMdd}-" +
            $"{suffix}";
    }
}
