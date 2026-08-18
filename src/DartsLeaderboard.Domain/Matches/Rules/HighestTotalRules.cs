using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed class HighestTotalRules : IGameRules
{
    private readonly int _roundLimit;

    public HighestTotalRules(int roundLimit) => _roundLimit = roundLimit;

    public string Title => $"Максимум за {_roundLimit} раундов";

    public bool SupportsTrendChart => true;

    public Result ValidateThrow(Match match, MatchParticipant participant, int points) => Result.Success();

    public MatchOutcome Evaluate(Match match) => MatchOutcome.NotFinished;

    public int? RunningValueAfterRound(Match match, MatchParticipant participant, int roundNumber) => null;

    public IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant) =>
        Array.Empty<StatisticItem>();

    public IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match) =>
        Array.Empty<StatisticItem>();
}
