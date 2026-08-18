using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches;

public sealed record MatchSettings(GameMode Mode, int? StartingScore, int? RoundLimit)
{
    public const int MaxStartingScore = 2000;
    public const int MaxRoundLimit = 50;

    public static MatchSettings X01(int startingScore) => new(GameMode.X01, startingScore, null);

    public static MatchSettings HighestTotal(int roundLimit) => new(GameMode.HighestTotal, null, roundLimit);

    public Result Validate() => Mode switch
    {
        GameMode.X01 when StartingScore is null or < 1 or > MaxStartingScore
            => Result.Failure(DomainErrorCode.InvalidStartingScore),
        GameMode.HighestTotal when RoundLimit is null or < 1 or > MaxRoundLimit
            => Result.Failure(DomainErrorCode.InvalidRoundLimit),
        _ => Result.Success()
    };
}
