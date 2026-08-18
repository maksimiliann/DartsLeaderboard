using DartsLeaderboard.Application.Abstractions;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);
}
