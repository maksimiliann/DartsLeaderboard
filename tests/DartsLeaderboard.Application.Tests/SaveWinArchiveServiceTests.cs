using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Reports;
using DartsLeaderboard.Application.Tests.Fakes;

namespace DartsLeaderboard.Application.Tests;

public class SaveWinArchiveServiceTests
{
    [Fact]
    public async Task ExecuteAsync_RejectsNegativeWins()
    {
        var service = new SaveWinArchiveService(new StubWinArchiveQueries());
        var rows = new[] { new WinArchiveRowDto(1, "Игрок", -1, 0) };

        var result = await service.ExecuteAsync(rows);

        Assert.False(result.IsSuccess);
        Assert.Equal("Количество побед в архиве не может быть отрицательным", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_SavesNonNegativeWins()
    {
        var queries = new CapturingWinArchiveQueries();
        var service = new SaveWinArchiveService(queries);
        var rows = new[] { new WinArchiveRowDto(1, "Игрок", 10, 4) };

        var result = await service.ExecuteAsync(rows);

        Assert.True(result.IsSuccess);
        Assert.Equal(rows, queries.Saved);
    }

    private sealed class CapturingWinArchiveQueries : StubWinArchiveQueries
    {
        public IReadOnlyList<WinArchiveRowDto>? Saved { get; private set; }

        public override Task SaveAsync(IReadOnlyList<WinArchiveRowDto> rows, CancellationToken cancellationToken)
        {
            Saved = rows;
            return Task.CompletedTask;
        }
    }
}
