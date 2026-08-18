namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed record MatchOutcome(bool IsFinished, int? WinnerParticipantId)
{
    public static readonly MatchOutcome NotFinished = new(false, null);
}
