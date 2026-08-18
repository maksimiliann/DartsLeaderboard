using System.Globalization;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed class X01Rules : IGameRules
{
    private static readonly NumberFormatInfo Ru = new() { NumberDecimalSeparator = "," };

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

    public IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant)
    {
        var items = new List<StatisticItem>
        {
            new("Осталось", RemainingFor(match, participant).ToString(Ru)),
            new("Раундов", match.RoundCountOf(participant.Id).ToString(Ru))
        };

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

    internal int RemainingFor(Match match, MatchParticipant participant) =>
        _startingScore - match.PointsOf(participant.Id);
}
