using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Domain.Tests;

public class HighestTotalRulesTests
{
    private static Match NewMatch(int roundLimit = 2, int participants = 2) =>
        MatchTestFactory.Create(MatchSettings.HighestTotal(roundLimit), participants);

    [Fact]
    public void Evaluate_NotFinishedUntilRoundLimitReachedByEveryone()
    {
        var match = NewMatch().WithRawThrows(60, 40, 20);

        Assert.False(match.Rules.Evaluate(match).IsFinished);
    }

    [Fact]
    public void Evaluate_WinnerIsHighestTotal()
    {
        var match = NewMatch().WithRawThrows(60, 40, 20, 30);

        var outcome = match.Rules.Evaluate(match);

        Assert.True(outcome.IsFinished);
        Assert.Equal(match.Participants[0].Id, outcome.WinnerParticipantId);
    }

    [Fact]
    public void Evaluate_TieHasNoWinner()
    {
        var match = NewMatch().WithRawThrows(60, 60, 20, 20);

        var outcome = match.Rules.Evaluate(match);

        Assert.True(outcome.IsFinished);
        Assert.Null(outcome.WinnerParticipantId);
    }

    [Fact]
    public void RunningValueAfterRound_IsCumulativeSum()
    {
        var match = NewMatch().WithRawThrows(60, 40, 25, 30);
        var first = match.Participants[0];

        Assert.Equal(60, match.Rules.RunningValueAfterRound(match, first, 1));
        Assert.Equal(85, match.Rules.RunningValueAfterRound(match, first, 2));
    }

    [Fact]
    public void Title_MentionsRoundLimit()
    {
        Assert.Equal("Максимум за 5 раундов", MatchTestFactory
            .Create(MatchSettings.HighestTotal(5), 2)
            .Rules.Title);
    }
}
