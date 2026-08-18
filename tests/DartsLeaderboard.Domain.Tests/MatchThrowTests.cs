using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Domain.Tests;

public class MatchThrowTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    private static Match X01(int startingScore = 301, int participants = 2) =>
        MatchTestFactory.Create(MatchSettings.X01(startingScore), participants);

    [Fact]
    public void RecordThrow_AssignsRoundAndParticipantInTurnOrder()
    {
        var match = X01();

        var first = match.RecordThrow(60, Now).Value!;
        var second = match.RecordThrow(45, Now).Value!;
        var third = match.RecordThrow(20, Now).Value!;

        Assert.Equal(match.Participants[0].Id, first.ParticipantId);
        Assert.Equal(1, first.RoundNumber);
        Assert.Equal(match.Participants[1].Id, second.ParticipantId);
        Assert.Equal(1, second.RoundNumber);
        Assert.Equal(match.Participants[0].Id, third.ParticipantId);
        Assert.Equal(2, third.RoundNumber);
        Assert.Equal(2, match.CurrentRoundNumber);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(181)]
    public void RecordThrow_RejectsPointsOutOfRange(int points)
    {
        var result = X01().RecordThrow(points, Now);

        Assert.Equal(DomainErrorCode.PointsOutOfRange, result.Error);
    }

    [Fact]
    public void RecordThrow_ZeroIsAllowed()
    {
        var match = X01();

        Assert.True(match.RecordThrow(0, Now).IsSuccess);
        Assert.Equal(301, match.Rules.RunningValueAfterRound(match, match.Participants[0], 1));
    }

    [Fact]
    public void RecordThrow_RejectsPointsAboveRemaining()
    {
        var match = X01(101);
        match.RecordThrow(100, Now);
        match.RecordThrow(50, Now);

        var result = match.RecordThrow(20, Now);

        Assert.Equal(DomainErrorCode.PointsExceedRemaining, result.Error);
    }

    [Fact]
    public void RecordThrow_FinishesMatchWhenRemainderIsZero()
    {
        var match = X01(101);
        match.RecordThrow(101, Now);

        Assert.Equal(MatchStatus.Finished, match.Status);
        Assert.Equal(Now, match.FinishedAt);
        Assert.Equal(match.Participants[0].Id, match.WinnerParticipantId);
        Assert.Null(match.CurrentParticipant);
    }

    [Fact]
    public void RecordThrow_RejectedAfterMatchFinished()
    {
        var match = X01(101);
        match.RecordThrow(101, Now);

        var result = match.RecordThrow(20, Now);

        Assert.Equal(DomainErrorCode.MatchNotInProgress, result.Error);
    }

    [Fact]
    public void UndoLastThrow_RemovesThrowAndReopensMatch()
    {
        var match = X01(101);
        match.RecordThrow(101, Now);

        var result = match.UndoLastThrow();

        Assert.True(result.IsSuccess);
        Assert.Empty(match.Throws);
        Assert.Equal(MatchStatus.InProgress, match.Status);
        Assert.Null(match.FinishedAt);
        Assert.Null(match.WinnerParticipantId);
        Assert.Equal(match.Participants[0].Id, match.CurrentParticipant!.Id);
    }

    [Fact]
    public void UndoLastThrow_FailsWhenNoThrows()
    {
        Assert.Equal(DomainErrorCode.NoThrowsToUndo, X01().UndoLastThrow().Error);
    }

    [Fact]
    public void Abandon_MarksMatchAbandoned()
    {
        var match = X01();

        var result = match.Abandon(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Abandoned, match.Status);
        Assert.Equal(Now, match.FinishedAt);
        Assert.Equal(DomainErrorCode.MatchNotInProgress, match.RecordThrow(20, Now).Error);
    }

    [Fact]
    public void CompletedRoundCount_CountsRecordedRounds()
    {
        var match = X01().WithThrows(60, 40, 20);

        Assert.Equal(2, match.CompletedRoundCount);
    }
}
