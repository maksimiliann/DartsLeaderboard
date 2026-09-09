namespace DartsLeaderboard.Application.Contracts;

public sealed record WeeklyHighlightDto(
    string Key,
    string Title,
    string Description,
    string? HolderName,
    string? Value);

public sealed record WeeklyHighlightsDto(
    DateTimeOffset WeekStart,
    DateTimeOffset WeekEnd,
    IReadOnlyList<WeeklyHighlightDto> Items);

public sealed record WeeklyPlayerFacts(
    string Name,
    int Wins,
    DateTimeOffset WinsAt,
    int Losses,
    DateTimeOffset LossesAt,
    int WinsX01,
    DateTimeOffset WinsX01At,
    int WinsHighestTotal,
    DateTimeOffset WinsHighestTotalAt,
    int? BestVisit,
    DateTimeOffset? BestVisitAt,
    int? WorstVisit,
    DateTimeOffset? WorstVisitAt,
    int? HighestTotalSum,
    DateTimeOffset? HighestTotalSumAt,
    int Count26,
    DateTimeOffset Count26At,
    int Count21,
    DateTimeOffset Count21At,
    int? FastestX01Rounds,
    DateTimeOffset? FastestX01At,
    int? SlowestX01Rounds,
    DateTimeOffset? SlowestX01At);

public sealed record WeeklyMatchFacts(
    string Participants,
    TimeSpan Duration,
    DateTimeOffset FinishedAt);
