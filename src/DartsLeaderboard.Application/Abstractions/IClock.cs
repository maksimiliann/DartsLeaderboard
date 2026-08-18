namespace DartsLeaderboard.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
