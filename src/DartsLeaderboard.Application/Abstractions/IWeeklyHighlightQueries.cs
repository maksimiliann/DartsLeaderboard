using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Abstractions;

public interface IWeeklyHighlightQueries
{
    Task<IReadOnlyList<WeeklyHighlightDto>> GetAsync(
        DateTimeOffset weekStart,
        DateTimeOffset weekEnd,
        CancellationToken cancellationToken);
}
