using DartsLeaderboard.Application.Matches;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Tests;

public class MatchStateMapperTests
{
    [Fact]
    public void ToDto_X01_BuildsColumnsRowsAndRemainders()
    {
        var match = TestMatchBuilder
            .Create(MatchSettings.X01(301), "Максим", "Аня")
            .WithThrows(60, 45, 100);

        var dto = MatchStateMapper.ToDto(match);

        Assert.Equal(12, dto.MatchId);
        Assert.Equal("301 на очки", dto.ModeTitle);
        Assert.Equal("Идёт", dto.StatusTitle);
        Assert.True(dto.IsInProgress);
        Assert.Equal(new[] { "Максим", "Аня" }, dto.Columns.Select(c => c.PlayerName));
        Assert.Equal(2, dto.Rows.Count);
        Assert.Equal(60, dto.Rows[0].Cells[0].Points);
        Assert.Equal(241, dto.Rows[0].Cells[0].RunningValue);
        Assert.Equal(100, dto.Rows[1].Cells[0].Points);
        Assert.Equal(141, dto.Rows[1].Cells[0].RunningValue);
        Assert.Null(dto.Rows[1].Cells[1].Points);
        Assert.Equal("Аня", dto.CurrentPlayerName);
        Assert.False(dto.ShowTrendChart);
        Assert.Empty(dto.ChartSeries);
        Assert.Empty(dto.Standings);
    }

    [Fact]
    public void ToDto_X01_ShowsWinnerAndFinishedStatus()
    {
        var match = TestMatchBuilder
            .Create(MatchSettings.X01(101), "Максим", "Аня")
            .WithThrows(101);

        var dto = MatchStateMapper.ToDto(match);

        Assert.False(dto.IsInProgress);
        Assert.Equal("Завершён", dto.StatusTitle);
        Assert.Equal("Максим", dto.WinnerPlayerName);
        Assert.Null(dto.CurrentParticipantId);
    }

    [Fact]
    public void ToDto_HighestTotal_BuildsChartSeries()
    {
        var match = TestMatchBuilder
            .Create(MatchSettings.HighestTotal(3), "Максим", "Аня")
            .WithThrows(60, 20, 40, 10);

        var dto = MatchStateMapper.ToDto(match);

        Assert.True(dto.ShowTrendChart);
        Assert.Equal(2, dto.ChartSeries.Count);
        Assert.Equal("Максим", dto.ChartSeries[0].PlayerName);
        Assert.Equal(new double[] { 60, 40 }, dto.ChartSeries[0].RoundPoints);
        Assert.Equal(new double[] { 60, 100 }, dto.ChartSeries[0].CumulativePoints);
        Assert.Contains(dto.MatchStatistics, s => s.Name == "Лучший бросок" && s.Value == "60 · Максим");
        Assert.Equal(2, dto.Standings.Count);
        Assert.Equal(1, dto.Standings[0].Place);
        Assert.Equal("Максим", dto.Standings[0].PlayerName);
        Assert.Equal(100, dto.Standings[0].Total);
        Assert.Equal(2, dto.Standings[1].Place);
        Assert.Equal("Аня", dto.Standings[1].PlayerName);
        Assert.Equal(30, dto.Standings[1].Total);
    }

    [Fact]
    public void ToDto_HighestTotal_TieShowsDrawStatus()
    {
        var match = TestMatchBuilder
            .Create(MatchSettings.HighestTotal(1), "Максим", "Аня")
            .WithThrows(60, 60);

        var dto = MatchStateMapper.ToDto(match);

        Assert.Equal("Ничья", dto.StatusTitle);
        Assert.Null(dto.WinnerPlayerName);
    }
}
