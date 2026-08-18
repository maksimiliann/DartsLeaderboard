namespace DartsLeaderboard.Application.Contracts;

public sealed record LeaderboardRowDto(int PlayerId, string PlayerName, int Wins, int MatchesPlayed)
{
    public double WinRate => MatchesPlayed == 0 ? 0 : (double)Wins / MatchesPlayed;
}
