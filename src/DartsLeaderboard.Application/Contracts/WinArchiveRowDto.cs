namespace DartsLeaderboard.Application.Contracts;

public sealed record WinArchiveRowDto(int PlayerId, string PlayerName, int WinsX01, int WinsHighestTotal);
