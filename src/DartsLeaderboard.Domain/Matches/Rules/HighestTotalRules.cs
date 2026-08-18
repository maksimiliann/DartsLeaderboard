using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed class HighestTotalRules : IGameRules
{
    private readonly int _roundLimit;

    public HighestTotalRules(int roundLimit) => _roundLimit = roundLimit;

    public string Title => $"Максимум за {_roundLimit} раундов";

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

    public IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant) =>
        Array.Empty<StatisticItem>();

    public IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match) =>
        Array.Empty<StatisticItem>();
}
