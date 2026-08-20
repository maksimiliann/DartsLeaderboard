using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Reports;

namespace DartsLeaderboard.Application.Tests;

public class ClubRecordsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void From_EmptyCandidates_LeavesRecordsUnset()
    {
        var records = ClubRecords.From([], []);

        Assert.Equal(new[] { ClubRecords.BestThrowKey, ClubRecords.BestFiveKey }, records.Select(r => r.Key));
        Assert.All(records, r =>
        {
            Assert.Null(r.HolderName);
            Assert.Null(r.Value);
        });
    }

    [Fact]
    public void From_PicksHighestThrow()
    {
        var records = ClubRecords.From(
            [
                new RecordCandidate("Анна", 140, T0),
                new RecordCandidate("Иван", 180, T0.AddMinutes(1))
            ],
            []);

        var best = records.Single(r => r.Key == ClubRecords.BestThrowKey);
        Assert.Equal("Иван", best.HolderName);
        Assert.Equal(180, best.Value);
    }

    [Fact]
    public void From_KeepsEarlierHolderWhenValueIsTied()
    {
        var records = ClubRecords.From(
            [
                new RecordCandidate("Анна", 180, T0),
                new RecordCandidate("Иван", 180, T0.AddMinutes(1))
            ],
            []);

        Assert.Equal("Анна", records.Single(r => r.Key == ClubRecords.BestThrowKey).HolderName);
    }

    [Fact]
    public void From_PicksHighestFiveRoundTotal()
    {
        var records = ClubRecords.From(
            [],
            [
                new RecordCandidate("Анна", 100, T0),
                new RecordCandidate("Иван", 125, T0.AddHours(1))
            ]);

        var best = records.Single(r => r.Key == ClubRecords.BestFiveKey);
        Assert.Equal("Иван", best.HolderName);
        Assert.Equal(125, best.Value);
        Assert.Equal("Лучший матч", best.Title);
    }
}
