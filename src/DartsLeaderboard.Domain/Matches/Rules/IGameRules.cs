using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches.Rules;

public interface IGameRules
{
    string Title { get; }

    bool SupportsTrendChart { get; }

    Result ValidateThrow(Match match, MatchParticipant participant, int points);

    int NormalizeVisit(Match match, MatchParticipant participant, IReadOnlyList<VisitDart> darts);

    MatchOutcome Evaluate(Match match);

    int? RunningValueAfterRound(Match match, MatchParticipant participant, int roundNumber);

    IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant);

    IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match);

    IReadOnlyList<StandingItem> BuildStandings(Match match);
}
