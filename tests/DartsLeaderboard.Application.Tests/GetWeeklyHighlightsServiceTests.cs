using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Reports;
using DartsLeaderboard.Application.Tests.Fakes;

namespace DartsLeaderboard.Application.Tests;

public class GetWeeklyHighlightsServiceTests
{
    [Fact]
    public async Task Execute_PassesCurrentMoscowWeekToQueries()
    {
        var queries = new CaptureWeeklyHighlightQueries();
        var clock = new FixedClock { UtcNow = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero) };
        var service = new GetWeeklyHighlightsService(queries, clock);

        var result = await service.ExecuteAsync();

        Assert.Equal(new DateTimeOffset(2026, 9, 6, 21, 0, 0, TimeSpan.Zero), queries.WeekStart);
        Assert.Equal(new DateTimeOffset(2026, 9, 13, 21, 0, 0, TimeSpan.Zero), queries.WeekEnd);
        Assert.Equal(queries.WeekStart, result.WeekStart);
        Assert.Equal(queries.WeekEnd, result.WeekEnd);
        Assert.Same(queries.Items, result.Items);
    }

    private sealed class CaptureWeeklyHighlightQueries : IWeeklyHighlightQueries
    {
        public DateTimeOffset WeekStart { get; private set; }

        public DateTimeOffset WeekEnd { get; private set; }

        public IReadOnlyList<WeeklyHighlightDto> Items { get; } =
            [new WeeklyHighlightDto("most-wins", "наныл недели", "Больше всех побед за неделю во всех режимах", "Анна", "2")];

        public Task<IReadOnlyList<WeeklyHighlightDto>> GetAsync(
            DateTimeOffset weekStart,
            DateTimeOffset weekEnd,
            CancellationToken cancellationToken)
        {
            WeekStart = weekStart;
            WeekEnd = weekEnd;
            return Task.FromResult(Items);
        }
    }
}
