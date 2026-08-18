using DartsLeaderboard.Domain.Players;

namespace DartsLeaderboard.Domain.Matches;

public sealed class MatchParticipant
{
    private MatchParticipant() { }

    internal MatchParticipant(int playerId, int seatOrder)
    {
        PlayerId = playerId;
        SeatOrder = seatOrder;
    }

    public int Id { get; private set; }
    public int MatchId { get; private set; }
    public int PlayerId { get; private set; }
    public int SeatOrder { get; private set; }
    public Player? Player { get; private set; }

    public string PlayerName => Player?.Name ?? $"Игрок {SeatOrder + 1}";
}
