namespace DartsLeaderboard.Application.Contracts;

public enum MatchListFilter
{
    InProgress = 1,
    Finished = 2
}

public sealed record MatchListItemDto(
    int MatchId,
    string ModeTitle,
    string Participants,
    string? WinnerName,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt);
