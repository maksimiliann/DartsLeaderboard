namespace DartsLeaderboard.Domain.Matches.Rules;

public static class GameRules
{
    public static IGameRules For(Match match) => match.Mode switch
    {
        GameMode.X01 => new X01Rules(match.StartingScore!.Value),
        GameMode.HighestTotal => new HighestTotalRules(match.RoundLimit!.Value),
        _ => throw new NotSupportedException($"Режим {match.Mode} не поддерживается")
    };
}
