using HomeAffairs.Application.Abstractions.Time;

namespace HomeAffairs.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
