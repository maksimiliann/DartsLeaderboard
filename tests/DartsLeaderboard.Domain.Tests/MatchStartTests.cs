using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Domain.Tests;

public class MatchStartTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_X01_SetsSettingsAndSeats()
    {
        var result = Match.Start(MatchSettings.X01(501), new[] { 7, 3, 9 }, Now);

        Assert.True(result.IsSuccess);
        var match = result.Value!;
        Assert.Equal(GameMode.X01, match.Mode);
        Assert.Equal(501, match.StartingScore);
        Assert.Null(match.RoundLimit);
        Assert.Equal(MatchStatus.InProgress, match.Status);
        Assert.Equal(Now, match.StartedAt);
        Assert.Equal(new[] { 7, 3, 9 }, match.Participants.Select(p => p.PlayerId));
        Assert.Equal(new[] { 0, 1, 2 }, match.Participants.Select(p => p.SeatOrder));
        Assert.Equal(1, match.CurrentRoundNumber);
        Assert.Equal(7, match.CurrentParticipant!.PlayerId);
    }

    [Fact]
    public void Start_HighestTotal_SetsRoundLimit()
    {
        var match = Match.Start(MatchSettings.HighestTotal(5), new[] { 1, 2 }, Now).Value!;

        Assert.Equal(GameMode.HighestTotal, match.Mode);
        Assert.Equal(5, match.RoundLimit);
        Assert.Null(match.StartingScore);
    }

    [Fact]
    public void Start_RejectsSinglePlayer()
    {
        var result = Match.Start(MatchSettings.X01(501), new[] { 1 }, Now);

        Assert.Equal(DomainErrorCode.TooFewParticipants, result.Error);
    }

    [Fact]
    public void Start_RejectsDuplicatePlayer()
    {
        var result = Match.Start(MatchSettings.X01(501), new[] { 1, 1 }, Now);

        Assert.Equal(DomainErrorCode.DuplicateParticipant, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(2001)]
    public void Start_RejectsInvalidStartingScore(int startingScore)
    {
        var result = Match.Start(MatchSettings.X01(startingScore), new[] { 1, 2 }, Now);

        Assert.Equal(DomainErrorCode.InvalidStartingScore, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Start_RejectsInvalidRoundLimit(int roundLimit)
    {
        var result = Match.Start(MatchSettings.HighestTotal(roundLimit), new[] { 1, 2 }, Now);

        Assert.Equal(DomainErrorCode.InvalidRoundLimit, result.Error);
    }
}
