using System.Globalization;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed class X01Rules : IGameRules
{
    private static readonly NumberFormatInfo Ru = new() { NumberDecimalSeparator = "," };

    private readonly int _startingScore;

    public X01Rules(int startingScore) => _startingScore = startingScore;

    public string Title => GameRules.TitleFor(GameMode.X01, _startingScore, null);

    public bool SupportsTrendChart => false;

    public Result ValidateThrow(Match match, MatchParticipant participant, int points) => Result.Success();

    public int NormalizeVisit(Match match, MatchParticipant participant, IReadOnlyList<VisitDart> darts)
    {
        var remaining = RemainingFor(match, participant);
        var scored = 0;

        foreach (var dart in darts)
        {
            if (dart.Points > remaining)
            {
                return 0;
            }

            var leftover = remaining - dart.Points;
            if (leftover == 1)
            {
                return 0;
            }

            if (leftover == 0)
            {
                return dart.IsDouble ? scored + dart.Points : 0;
            }

            remaining = leftover;
            scored += dart.Points;
        }

        return scored;
    }

    public MatchOutcome Evaluate(Match match)
    {
        var winner = match.Participants.FirstOrDefault(p => RemainingFor(match, p) == 0);
        return winner is null ? MatchOutcome.NotFinished : new MatchOutcome(true, winner.Id);
    }

    public int? RunningValueAfterRound(Match match, MatchParticipant participant, int roundNumber) =>
        _startingScore - match.Throws
            .Where(t => t.ParticipantId == participant.Id && t.RoundNumber <= roundNumber)
            .Sum(t => t.Points);

    public IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant)
    {
        var remaining = RemainingFor(match, participant);
        var items = new List<StatisticItem>
        {
            new("Осталось", remaining.ToString(Ru))
        };

        if (DoubleOutHint(remaining) is { } hint)
        {
            items.Add(new StatisticItem("Удвоение", hint));
        }

        items.Add(new StatisticItem("Раундов", match.RoundCountOf(participant.Id).ToString(Ru)));

        if (match.WinnerParticipantId == participant.Id)
        {
            items.Add(new StatisticItem("Закрыл за", match.RoundCountOf(participant.Id).ToString(Ru)));
        }

        return items;
    }

    public IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match)
    {
        var leader = match.Participants
            .OrderBy(p => RemainingFor(match, p))
            .ThenBy(p => p.SeatOrder)
            .First();

        return new List<StatisticItem>
        {
            new("Раундов сыграно", match.CompletedRoundCount.ToString(Ru)),
            new("Ближе всех к финишу", $"{leader.PlayerName} ({RemainingFor(match, leader)})")
        };
    }

    public IReadOnlyList<StandingItem> BuildStandings(Match match) => Array.Empty<StandingItem>();

    internal int RemainingFor(Match match, MatchParticipant participant) =>
        _startingScore - match.PointsOf(participant.Id);

    internal static string? DoubleOutHint(int remaining)
    {
        if (remaining == 50)
        {
            return "50";
        }

        return remaining > 0 && remaining < 40 && remaining % 2 == 0
            ? (remaining / 2).ToString(Ru)
            : null;
    }
}
