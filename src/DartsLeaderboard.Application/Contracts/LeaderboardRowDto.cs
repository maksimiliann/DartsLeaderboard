namespace DartsLeaderboard.Application.Contracts;

public sealed record LeaderboardRowDto(
    int PlayerId,
    string PlayerName,
    int WinsX01,
    int WinsHighestTotal,
    int MatchesPlayed,
    int LiveWins)
{
    public int Wins => WinsX01 + WinsHighestTotal;

    public double WinRate => MatchesPlayed == 0 ? 0 : (double)LiveWins / MatchesPlayed;
}
