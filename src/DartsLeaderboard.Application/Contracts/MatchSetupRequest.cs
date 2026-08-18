namespace DartsLeaderboard.Application.Contracts;

public enum GameModeOption
{
    X01 = 1,
    HighestTotal = 2
}

public sealed record MatchSetupRequest(
    GameModeOption Mode,
    int? StartingScore,
    int? RoundLimit,
    IReadOnlyList<int> PlayerIds);
