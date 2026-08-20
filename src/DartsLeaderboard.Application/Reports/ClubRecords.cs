using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Reports;

public static class ClubRecords
{
    public const string BestThrowKey = "best-throw";
    public const string BestFiveKey = "best-five";

    public const string BestThrowTitle = "Лучший бросок";
    public const string BestFiveTitle = "Лучший матч";

    public static IReadOnlyList<ClubRecordDto> From(
        IReadOnlyList<RecordCandidate> throws,
        IReadOnlyList<RecordCandidate> fiveRoundTotals) =>
        [
            Pick(BestThrowKey, BestThrowTitle, throws),
            Pick(BestFiveKey, BestFiveTitle, fiveRoundTotals)
        ];

    private static ClubRecordDto Pick(string key, string title, IReadOnlyList<RecordCandidate> candidates)
    {
        var best = candidates
            .OrderByDescending(c => c.Value)
            .ThenBy(c => c.AchievedAt)
            .FirstOrDefault();

        return best is null
            ? new ClubRecordDto(key, title, null, null)
            : new ClubRecordDto(key, title, best.HolderName, best.Value);
    }
}
