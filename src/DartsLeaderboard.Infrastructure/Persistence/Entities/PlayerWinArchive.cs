namespace DartsLeaderboard.Infrastructure.Persistence.Entities;

public sealed class PlayerWinArchive
{
    public int PlayerId { get; set; }

    public int WinsX01 { get; set; }

    public int WinsHighestTotal { get; set; }
}
