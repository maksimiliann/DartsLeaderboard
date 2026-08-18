namespace DartsLeaderboard.Domain.Matches;

public sealed class Throw
{
    private Throw() { }

    internal Throw(int participantId, int roundNumber, int points, DateTimeOffset recordedAt)
    {
        ParticipantId = participantId;
        RoundNumber = roundNumber;
        Points = points;
        RecordedAt = recordedAt;
    }

    public const int MaxPoints = 180;

    public int Id { get; private set; }
    public int MatchId { get; private set; }
    public int ParticipantId { get; private set; }
    public int RoundNumber { get; private set; }
    public int Points { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
}
