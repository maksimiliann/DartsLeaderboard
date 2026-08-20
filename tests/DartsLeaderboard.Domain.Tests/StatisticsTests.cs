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

    [Theory]
    [InlineData(50, "50")]
    [InlineData(38, "38")]
    [InlineData(32, "32")]
    [InlineData(2, "2")]
    public void X01_PlayerStatistics_ShowDoubleOutHint(int remaining, string expected)
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(remaining), 2);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.Equal(expected, Value(stats, "Удвоение"));
    }

    [Theory]
    [InlineData(40)]
    [InlineData(41)]
    [InlineData(301)]
    [InlineData(1)]
    public void X01_PlayerStatistics_OmitDoubleOutHintWhenNotCheckoutDouble(int remaining)
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(remaining), 2);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.DoesNotContain(stats, i => i.Name == "Удвоение");
    }

    [Fact]
    public void X01_PlayerStatistics_OmitDoubleOutHintWhenClosed()
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(101), 2).WithThrows(101);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.DoesNotContain(stats, i => i.Name == "Удвоение");
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
        Assert.DoesNotContain(stats, i => i.Name is "Больше 100" or "Меньше 10" or "Очко" or "Классика");
    }

    [Fact]
    public void HighestTotal_MatchStatistics_CountSpecialThrows()
    {
        var match = MatchTestFactory.Create(MatchSettings.HighestTotal(5), 2)
            .WithThrows(140, 10, 21, 10, 26, 10, 5, 10, 100, 10);
        var stats = match.Rules.BuildMatchStatistics(match);

        Assert.Equal("1", Value(stats, "Больше 100"));
        Assert.Equal("1", Value(stats, "Меньше 10"));
        Assert.Equal("1", Value(stats, "Очко"));
        Assert.Equal("1", Value(stats, "Классика"));
    }

    [Fact]
    public void HighestTotal_Standings_RankByCurrentTotal()
    {
        var match = MatchTestFactory.Create(MatchSettings.HighestTotal(1), 3)
            .WithThrows(80, 40, 100);
        var standings = match.Rules.BuildStandings(match);

        Assert.Equal(new[] { 1, 2, 3 }, standings.Select(s => s.Place));
        Assert.Equal(new[] { "Игрок 3", "Игрок 1", "Игрок 2" }, standings.Select(s => s.PlayerName));
        Assert.Equal(new[] { 100, 80, 40 }, standings.Select(s => s.Total));
    }

    [Fact]
    public void HighestTotal_Standings_SharePlaceOnTiedTotal()
    {
        var match = MatchTestFactory.Create(MatchSettings.HighestTotal(1), 3)
            .WithThrows(80, 80, 40);
        var standings = match.Rules.BuildStandings(match);

        Assert.Equal(new[] { 1, 1, 3 }, standings.Select(s => s.Place));
        Assert.Equal(new[] { "Игрок 1", "Игрок 2", "Игрок 3" }, standings.Select(s => s.PlayerName));
        Assert.Equal(new[] { 80, 80, 40 }, standings.Select(s => s.Total));
    }

    [Fact]
    public void X01_Standings_AreEmpty()
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(301), 2);

        Assert.Empty(match.Rules.BuildStandings(match));
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
        Assert.Equal("0", Value(stats, "Больше 100"));
        Assert.Equal("1", Value(stats, "Меньше 10"));
        Assert.Equal("0", Value(stats, "Очко"));
        Assert.Equal("0", Value(stats, "Классика"));
    }

    [Fact]
    public void HighestTotal_Statistics_HandleMatchWithoutThrows()
    {
        var match = MatchTestFactory.Create(MatchSettings.HighestTotal(5), 2);
        var playerStats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);
        var matchStats = match.Rules.BuildMatchStatistics(match);

        Assert.Equal("0", Value(playerStats, "Сумма"));
        Assert.Equal("—", Value(playerStats, "Среднее"));
        Assert.DoesNotContain(playerStats, i => i.Name is "Больше 100" or "Меньше 10" or "Очко" or "Классика");
        Assert.Equal("—", Value(matchStats, "Лучший бросок"));
        Assert.Equal("5", Value(matchStats, "Осталось раундов"));
        Assert.Equal("0", Value(matchStats, "Больше 100"));
        Assert.Equal("0", Value(matchStats, "Меньше 10"));
        Assert.Equal("0", Value(matchStats, "Очко"));
        Assert.Equal("0", Value(matchStats, "Классика"));
    }
}
