using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Reports;

namespace DartsLeaderboard.Application.Tests;

public class WeeklyHighlightsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddHours(1);

    [Fact]
    public void From_EmptyFacts_LeavesAllHighlightsUnset()
    {
        var rows = WeeklyHighlights.From([], []);

        Assert.Equal(
            new[]
            {
                WeeklyHighlights.MostWinsKey,
                WeeklyHighlights.MostLossesKey,
                WeeklyHighlights.MostX01WinsKey,
                WeeklyHighlights.MostHighestTotalWinsKey,
                WeeklyHighlights.BestVisitKey,
                WeeklyHighlights.WorstVisitKey,
                WeeklyHighlights.MaxSumKey,
                WeeklyHighlights.MinSumKey,
                WeeklyHighlights.ClosestToAverageKey,
                WeeklyHighlights.Most26Key,
                WeeklyHighlights.Most21Key,
                WeeklyHighlights.FastestX01Key,
                WeeklyHighlights.SlowestX01Key,
                WeeklyHighlights.FastestMatchKey,
                WeeklyHighlights.LongestMatchKey
            },
            rows.Select(r => r.Key));
        Assert.Equal(
            new[]
            {
                WeeklyHighlights.MostWinsTitle,
                WeeklyHighlights.MostLossesTitle,
                WeeklyHighlights.MostX01WinsTitle,
                WeeklyHighlights.MostHighestTotalWinsTitle,
                WeeklyHighlights.BestVisitTitle,
                WeeklyHighlights.WorstVisitTitle,
                WeeklyHighlights.MaxSumTitle,
                WeeklyHighlights.MinSumTitle,
                WeeklyHighlights.ClosestToAverageTitle,
                WeeklyHighlights.Most26Title,
                WeeklyHighlights.Most21Title,
                WeeklyHighlights.FastestX01Title,
                WeeklyHighlights.SlowestX01Title,
                WeeklyHighlights.FastestMatchTitle,
                WeeklyHighlights.LongestMatchTitle
            },
            rows.Select(r => r.Title));
        Assert.Equal(
            new[]
            {
                WeeklyHighlights.MostWinsDescription,
                WeeklyHighlights.MostLossesDescription,
                WeeklyHighlights.MostX01WinsDescription,
                WeeklyHighlights.MostHighestTotalWinsDescription,
                WeeklyHighlights.BestVisitDescription,
                WeeklyHighlights.WorstVisitDescription,
                WeeklyHighlights.MaxSumDescription,
                WeeklyHighlights.MinSumDescription,
                WeeklyHighlights.ClosestToAverageDescription,
                WeeklyHighlights.Most26Description,
                WeeklyHighlights.Most21Description,
                WeeklyHighlights.FastestX01Description,
                WeeklyHighlights.SlowestX01Description,
                WeeklyHighlights.FastestMatchDescription,
                WeeklyHighlights.LongestMatchDescription
            },
            rows.Select(r => r.Description));
        Assert.All(rows, row =>
        {
            Assert.Null(row.HolderName);
            Assert.Null(row.Value);
        });
    }

    [Fact]
    public void From_PicksMaxWinsAndIgnoresZero()
    {
        var rows = WeeklyHighlights.From(
            [
                Player("Анна", wins: 2, at: T0),
                Player("Иван", wins: 3, at: T1),
                Player("Пётр", wins: 0, at: T0)
            ],
            []);

        var row = Single(rows, WeeklyHighlights.MostWinsKey);
        Assert.Equal("Иван", row.HolderName);
        Assert.Equal("3", row.Value);
    }

    [Fact]
    public void From_ListsAllHoldersWhenWinsTied()
    {
        var rows = WeeklyHighlights.From(
            [
                Player("Анна", wins: 2, at: T0),
                Player("Иван", wins: 2, at: T1)
            ],
            []);

        var row = Single(rows, WeeklyHighlights.MostWinsKey);
        Assert.Equal("Анна, Иван", row.HolderName);
        Assert.Equal("2", row.Value);
    }

    [Fact]
    public void From_PicksMaxLossesX01AndHighestTotalWins()
    {
        var rows = WeeklyHighlights.From(
            [
                Player("Анна", losses: 4, winsX01: 2, winsHighestTotal: 1, at: T0),
                Player("Иван", losses: 1, winsX01: 5, winsHighestTotal: 3, at: T1)
            ],
            []);

        Assert.Equal("Анна", Single(rows, WeeklyHighlights.MostLossesKey).HolderName);
        Assert.Equal("4", Single(rows, WeeklyHighlights.MostLossesKey).Value);
        Assert.Equal("Иван", Single(rows, WeeklyHighlights.MostX01WinsKey).HolderName);
        Assert.Equal("5", Single(rows, WeeklyHighlights.MostX01WinsKey).Value);
        Assert.Equal("Иван", Single(rows, WeeklyHighlights.MostHighestTotalWinsKey).HolderName);
        Assert.Equal("3", Single(rows, WeeklyHighlights.MostHighestTotalWinsKey).Value);
    }

    [Fact]
    public void From_PicksBestAndWorstHighestTotalVisits()
    {
        var rows = WeeklyHighlights.From(
            [
                Player("Анна", bestVisit: 140, worstVisit: 26, at: T0),
                Player("Иван", bestVisit: 180, worstVisit: 0, at: T1)
            ],
            []);

        Assert.Equal("Иван", Single(rows, WeeklyHighlights.BestVisitKey).HolderName);
        Assert.Equal("180", Single(rows, WeeklyHighlights.BestVisitKey).Value);
        Assert.Equal("Иван", Single(rows, WeeklyHighlights.WorstVisitKey).HolderName);
        Assert.Equal("0", Single(rows, WeeklyHighlights.WorstVisitKey).Value);
    }

    [Fact]
    public void From_PicksMaxMinAndClosestHighestTotalSums()
    {
        var rows = WeeklyHighlights.From(
            [
                Player("Анна", highestTotalSum: 100, at: T0),
                Player("Иван", highestTotalSum: 200, at: T1),
                Player("Пётр", highestTotalSum: 148, at: T1.AddMinutes(1))
            ],
            []);

        Assert.Equal("Иван", Single(rows, WeeklyHighlights.MaxSumKey).HolderName);
        Assert.Equal("200", Single(rows, WeeklyHighlights.MaxSumKey).Value);
        Assert.Equal("Анна", Single(rows, WeeklyHighlights.MinSumKey).HolderName);
        Assert.Equal("100", Single(rows, WeeklyHighlights.MinSumKey).Value);
        Assert.Equal("Пётр", Single(rows, WeeklyHighlights.ClosestToAverageKey).HolderName);
        Assert.Equal("148", Single(rows, WeeklyHighlights.ClosestToAverageKey).Value);
    }

    [Fact]
    public void From_ClosestToAverage_ListsAllWhenDistanceTied()
    {
        var rows = WeeklyHighlights.From(
            [
                Player("Анна", highestTotalSum: 100, at: T0),
                Player("Иван", highestTotalSum: 200, at: T1)
            ],
            []);

        var row = Single(rows, WeeklyHighlights.ClosestToAverageKey);
        Assert.Equal("Анна, Иван", row.HolderName);
        Assert.Equal("100, 200", row.Value);
    }

    [Fact]
    public void From_ListsAllHoldersWhenMost21Tied()
    {
        var rows = WeeklyHighlights.From(
            [
                Player("Анна", count21: 3, at: T0),
                Player("Иван", count21: 3, at: T1),
                Player("Пётр", count21: 3, at: T1.AddMinutes(1)),
                Player("Олег", count21: 1, at: T0)
            ],
            []);

        var row = Single(rows, WeeklyHighlights.Most21Key);
        Assert.Equal("Анна, Иван, Пётр", row.HolderName);
        Assert.Equal("3", row.Value);
    }

    [Fact]
    public void From_PicksMost26ThenMost21()
    {
        var rows = WeeklyHighlights.From(
            [
                Player("Анна", count26: 2, count21: 5, at: T0),
                Player("Иван", count26: 4, count21: 1, at: T1)
            ],
            []);

        var classic = Single(rows, WeeklyHighlights.Most26Key);
        var ochko = Single(rows, WeeklyHighlights.Most21Key);
        Assert.Equal(WeeklyHighlights.Most21Key, rows[10].Key);
        Assert.Equal("Иван", classic.HolderName);
        Assert.Equal("4", classic.Value);
        Assert.Equal("Анна", ochko.HolderName);
        Assert.Equal("5", ochko.Value);
    }

    [Fact]
    public void From_PicksFastestAndSlowestX01Checkouts()
    {
        var rows = WeeklyHighlights.From(
            [
                Player("Анна", fastestX01Rounds: 3, slowestX01Rounds: 12, at: T0),
                Player("Иван", fastestX01Rounds: 8, slowestX01Rounds: 9, at: T1)
            ],
            []);

        Assert.Equal("Анна", Single(rows, WeeklyHighlights.FastestX01Key).HolderName);
        Assert.Equal("3", Single(rows, WeeklyHighlights.FastestX01Key).Value);
        Assert.Equal("Анна", Single(rows, WeeklyHighlights.SlowestX01Key).HolderName);
        Assert.Equal("12", Single(rows, WeeklyHighlights.SlowestX01Key).Value);
    }

    [Fact]
    public void From_PicksFastestAndLongestMatches()
    {
        var rows = WeeklyHighlights.From(
            [],
            [
                new WeeklyMatchFacts("Анна, Иван", TimeSpan.FromSeconds(65), T1),
                new WeeklyMatchFacts("Пётр, Олег", TimeSpan.FromMinutes(12), T0)
            ]);

        Assert.Equal("Анна, Иван", Single(rows, WeeklyHighlights.FastestMatchKey).HolderName);
        Assert.Equal("1:05", Single(rows, WeeklyHighlights.FastestMatchKey).Value);
        Assert.Equal("Пётр, Олег", Single(rows, WeeklyHighlights.LongestMatchKey).HolderName);
        Assert.Equal("12:00", Single(rows, WeeklyHighlights.LongestMatchKey).Value);
    }

    [Fact]
    public void From_ListsAllParticipantsWhenMatchDurationTied()
    {
        var rows = WeeklyHighlights.From(
            [],
            [
                new WeeklyMatchFacts("Анна, Иван", TimeSpan.FromSeconds(65), T1),
                new WeeklyMatchFacts("Пётр, Олег", TimeSpan.FromSeconds(65), T0)
            ]);

        var row = Single(rows, WeeklyHighlights.FastestMatchKey);
        Assert.Equal("Анна, Иван, Пётр, Олег", row.HolderName);
        Assert.Equal("1:05", row.Value);
    }

    private static WeeklyHighlightDto Single(IReadOnlyList<WeeklyHighlightDto> rows, string key) =>
        rows.Single(row => row.Key == key);

    private static WeeklyPlayerFacts Player(
        string name,
        int wins = 0,
        int losses = 0,
        int winsX01 = 0,
        int winsHighestTotal = 0,
        int? bestVisit = null,
        int? worstVisit = null,
        int? highestTotalSum = null,
        int count26 = 0,
        int count21 = 0,
        int? fastestX01Rounds = null,
        int? slowestX01Rounds = null,
        DateTimeOffset? at = null)
    {
        var achieved = at ?? T0;
        return new WeeklyPlayerFacts(
            name,
            wins,
            achieved,
            losses,
            achieved,
            winsX01,
            achieved,
            winsHighestTotal,
            achieved,
            bestVisit,
            bestVisit is null ? null : achieved,
            worstVisit,
            worstVisit is null ? null : achieved,
            highestTotalSum,
            highestTotalSum is null ? null : achieved,
            count26,
            achieved,
            count21,
            achieved,
            fastestX01Rounds,
            fastestX01Rounds is null ? null : achieved,
            slowestX01Rounds,
            slowestX01Rounds is null ? null : achieved);
    }
}
