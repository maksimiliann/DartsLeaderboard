using DartsLeaderboard.Application.Abstractions;

namespace DartsLeaderboard.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
