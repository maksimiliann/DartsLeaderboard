using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Matches.Rules;

namespace DartsLeaderboard.Domain.Tests;

public class StatisticsTests
{
    private static string Value(IReadOnlyList<StatisticItem> items, string name) =>
        items.Single(i => i.Name == name).Value;

    [Fact]
    public void X01_PlayerStatistics_ShowRemainderAndRounds()
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(301), 2).WithThrows(60, 40, 100);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.Equal("141", Value(stats, "Осталось"));
        Assert.Equal("2", Value(stats, "Раундов"));
        Assert.DoesNotContain(stats, i => i.Name == "Закрыл за");
    }

    [Fact]
    public void X01_PlayerStatistics_ShowClosingRoundsForWinner()
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(101), 2).WithThrows(60, 20, 41);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.Equal("0", Value(stats, "Осталось"));
        Assert.Equal("2", Value(stats, "Закрыл за"));
    }

    [Fact]
    public void X01_PlayerStatistics_HaveNoThrowMetrics()
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(301), 2).WithThrows(60, 40);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.DoesNotContain(stats, i => i.Name is "Максимум" or "Минимум" or "Среднее");
    }

    [Fact]
    public void X01_MatchStatistics_ShowRoundsAndLeader()
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(301), 2).WithThrows(60, 40, 100);
        var stats = match.Rules.BuildMatchStatistics(match);

        Assert.Equal("2", Value(stats, "Раундов сыграно"));
        Assert.Equal("Игрок 1 (141)", Value(stats, "Ближе всех к финишу"));
    }

    [Fact]
    public void HighestTotal_PlayerStatistics_CountZerosInEveryMetric()
    {
        var match = MatchTestFactory.Create(MatchSettings.HighestTotal(3), 2)
            .WithThrows(60, 10, 0, 20, 30, 30);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.Equal("90", Value(stats, "Сумма"));
        Assert.Equal("60", Value(stats, "Максимум"));
        Assert.Equal("0", Value(stats, "Минимум"));
        Assert.Equal("30,0", Value(stats, "Среднее"));
        Assert.Equal("3", Value(stats, "Раундов"));
    }

    [Fact]
    public void HighestTotal_MatchStatistics_ShowBestAndWorstThrow()
    {
        var match = MatchTestFactory.Create(MatchSettings.HighestTotal(2), 2)
            .WithThrows(60, 10, 0, 20);
        var stats = match.Rules.BuildMatchStatistics(match);

        Assert.Equal("60 · Игрок 1", Value(stats, "Лучший бросок"));
        Assert.Equal("0 · Игрок 1", Value(stats, "Худший бросок"));
        Assert.Equal("22,5", Value(stats, "Средний раунд"));
        Assert.Equal("0", Value(stats, "Осталось раундов"));
    }

    [Fact]
    public void HighestTotal_Statistics_HandleMatchWithoutThrows()
    {
        var match = MatchTestFactory.Create(MatchSettings.HighestTotal(5), 2);
        var playerStats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);
        var matchStats = match.Rules.BuildMatchStatistics(match);

        Assert.Equal("0", Value(playerStats, "Сумма"));
        Assert.Equal("—", Value(playerStats, "Среднее"));
        Assert.Equal("—", Value(matchStats, "Лучший бросок"));
        Assert.Equal("5", Value(matchStats, "Осталось раундов"));
    }
}
