using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Matches.Rules;

namespace DartsLeaderboard.Domain.Tests;

public class X01RulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    private static Match NewMatch() =>
        Match.Start(MatchSettings.X01(301), new[] { 1, 2 }, Now).Value!;

    [Fact]
    public void For_ReturnsX01Rules()
    {
        Assert.IsType<X01Rules>(GameRules.For(NewMatch()));
    }

    [Fact]
    public void ValidateThrow_AllowsPointsUpToRemaining()
    {
        var match = NewMatch();
        var rules = match.Rules;

        Assert.True(rules.ValidateThrow(match, match.Participants[0], 180).IsSuccess);
    }

    [Fact]
    public void ValidateThrow_RejectsPointsAboveRemaining()
    {
        var match = NewMatch();
        var rules = new X01Rules(50);

        var result = rules.ValidateThrow(match, match.Participants[0], 60);

        Assert.Equal(DomainErrorCode.PointsExceedRemaining, result.Error);
    }

    [Fact]
    public void Evaluate_NotFinishedWhileEveryoneHasRemainder()
    {
        var match = NewMatch();

        Assert.False(match.Rules.Evaluate(match).IsFinished);
    }

    [Fact]
    public void SupportsTrendChart_IsFalse()
    {
        Assert.False(NewMatch().Rules.SupportsTrendChart);
    }

    [Fact]
    public void Title_MentionsStartingScore()
    {
        Assert.Equal("301 на очки", NewMatch().Rules.Title);
    }
}
