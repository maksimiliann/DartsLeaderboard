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
    public void NormalizeVisit_ZerosBustLeaveOneAndNonDoubleCheckout()
    {
        var match = Match.Start(MatchSettings.X01(50), new[] { 1, 2 }, Now).Value!;
        var rules = match.Rules;
        var player = match.Participants[0];

        Assert.Equal(0, rules.NormalizeVisit(match, player, [new VisitDart(60, false)]));
        Assert.Equal(0, rules.NormalizeVisit(match, player, [new VisitDart(49, false)]));
        Assert.Equal(0, rules.NormalizeVisit(match, player, [new VisitDart(50, false)]));
        Assert.Equal(50, rules.NormalizeVisit(match, player, [new VisitDart(50, true)]));
        Assert.Equal(20, rules.NormalizeVisit(match, player, [new VisitDart(20, false)]));
    }

    [Fact]
    public void NormalizeVisit_AllowsDoubleCheckoutOnAnyDart()
    {
        var match = Match.Start(MatchSettings.X01(40), new[] { 1, 2 }, Now).Value!;
        var rules = match.Rules;
        var player = match.Participants[0];

        Assert.Equal(40, rules.NormalizeVisit(match, player, [new VisitDart(40, true)]));
        Assert.Equal(40, rules.NormalizeVisit(match, player, [new VisitDart(20, false), new VisitDart(20, true)]));
        Assert.Equal(0, rules.NormalizeVisit(match, player, [new VisitDart(20, false), new VisitDart(20, false)]));
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
