using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed class X01Rules : IGameRules
{
    private readonly int _startingScore;

    public X01Rules(int startingScore) => _startingScore = startingScore;

    public string Title => $"{_startingScore} на очки";

    public bool SupportsTrendChart => false;

    public Result ValidateThrow(Match match, MatchParticipant participant, int points) =>
        points > RemainingFor(match, participant)
            ? Result.Failure(DomainErrorCode.PointsExceedRemaining)
            : Result.Success();

    public MatchOutcome Evaluate(Match match)
    {
        var winner = match.Participants.FirstOrDefault(p => RemainingFor(match, p) == 0);
        return winner is null ? MatchOutcome.NotFinished : new MatchOutcome(true, winner.Id);
    }

    public int? RunningValueAfterRound(Match match, MatchParticipant participant, int roundNumber) =>
        _startingScore - match.Throws
            .Where(t => t.ParticipantId == participant.Id && t.RoundNumber <= roundNumber)
            .Sum(t => t.Points);

    public IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant) =>
        Array.Empty<StatisticItem>();

    public IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match) =>
        Array.Empty<StatisticItem>();

    internal int RemainingFor(Match match, MatchParticipant participant) =>
        _startingScore - match.PointsOf(participant.Id);
}
