using System.Globalization;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed class HighestTotalRules : IGameRules
{
    private static readonly NumberFormatInfo Ru = new() { NumberDecimalSeparator = "," };
    private const string NoValue = "—";

    private readonly int _roundLimit;

    public HighestTotalRules(int roundLimit) => _roundLimit = roundLimit;

    public string Title => GameRules.TitleFor(GameMode.HighestTotal, null, _roundLimit);

    public bool SupportsTrendChart => true;

    public Result ValidateThrow(Match match, MatchParticipant participant, int points) => Result.Success();

    public MatchOutcome Evaluate(Match match)
    {
        var completedByEveryone = match.Participants.All(p => match.RoundCountOf(p.Id) >= _roundLimit);
        if (!completedByEveryone)
        {
            return MatchOutcome.NotFinished;
        }

        var totals = match.Participants
            .Select(p => new { Participant = p, Total = match.PointsOf(p.Id) })
            .ToList();

        var best = totals.Max(t => t.Total);
        var winners = totals.Where(t => t.Total == best).ToList();

        return winners.Count == 1
            ? new MatchOutcome(true, winners[0].Participant.Id)
            : new MatchOutcome(true, null);
    }

    public int? RunningValueAfterRound(Match match, MatchParticipant participant, int roundNumber) =>
        match.Throws
            .Where(t => t.ParticipantId == participant.Id && t.RoundNumber <= roundNumber)
            .Sum(t => t.Points);

    public IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant)
    {
        var points = match.Throws
            .Where(t => t.ParticipantId == participant.Id)
            .Select(t => t.Points)
            .ToList();

        return new List<StatisticItem>
        {
            new("Сумма", points.Sum().ToString(Ru)),
            new("Максимум", points.Count == 0 ? NoValue : points.Max().ToString(Ru)),
            new("Минимум", points.Count == 0 ? NoValue : points.Min().ToString(Ru)),
            new("Среднее", points.Count == 0 ? NoValue : points.Average().ToString("F1", Ru)),
            new("Раундов", points.Count.ToString(Ru))
        };
    }

    public IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match)
    {
        var throws = match.Throws;
        var remainingRounds = Math.Max(0, _roundLimit - match.CompletedRoundCount);
        var points = throws.Select(t => t.Points).ToList();

        if (throws.Count == 0)
        {
            return new List<StatisticItem>
            {
                new("Лучший бросок", NoValue),
                new("Худший бросок", NoValue),
                new("Средний раунд", NoValue),
                new("Осталось раундов", remainingRounds.ToString(Ru))
            }.Concat(ThrowCounts(points)).ToList();
        }

        var best = throws.OrderByDescending(t => t.Points).First();
        var worst = throws.OrderBy(t => t.Points).First();

        return new List<StatisticItem>
        {
            new("Лучший бросок", Describe(match, best)),
            new("Худший бросок", Describe(match, worst)),
            new("Средний раунд", throws.Average(t => t.Points).ToString("F1", Ru)),
            new("Осталось раундов", remainingRounds.ToString(Ru))
        }.Concat(ThrowCounts(points)).ToList();
    }

    public IReadOnlyList<StandingItem> BuildStandings(Match match)
    {
        var ordered = match.Participants
            .Select(p => (Participant: p, Total: match.PointsOf(p.Id)))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Participant.SeatOrder)
            .ToList();

        var standings = new List<StandingItem>(ordered.Count);
        var place = 1;
        for (var i = 0; i < ordered.Count; i++)
        {
            if (i > 0 && ordered[i].Total != ordered[i - 1].Total)
            {
                place = i + 1;
            }

            standings.Add(new StandingItem(place, ordered[i].Participant.PlayerName, ordered[i].Total));
        }

        return standings;
    }

    private static IReadOnlyList<StatisticItem> ThrowCounts(IReadOnlyList<int> points) =>
        new List<StatisticItem>
        {
            new("Больше 100", points.Count(p => p > 100).ToString(Ru)),
            new("Меньше 10", points.Count(p => p < 10).ToString(Ru)),
            new("Очко", points.Count(p => p == 21).ToString(Ru)),
            new("Классика", points.Count(p => p == 26).ToString(Ru))
        };

    private static string Describe(Match match, Throw recorded)
    {
        var participant = match.Participants.First(p => p.Id == recorded.ParticipantId);
        return $"{recorded.Points} · {participant.PlayerName}";
    }
}
