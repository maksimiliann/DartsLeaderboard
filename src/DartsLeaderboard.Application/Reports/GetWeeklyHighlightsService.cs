using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Reports;

public sealed class GetWeeklyHighlightsService(IWeeklyHighlightQueries queries, IClock clock)
{
    public async Task<WeeklyHighlightsDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var (start, end) = WeekRange.Containing(clock.UtcNow);
        var items = await queries.GetAsync(start, end, cancellationToken);
        return new WeeklyHighlightsDto(start, end, items);
    }
}
