namespace DartsLeaderboard.Application.Contracts;

public sealed record ThrowDartDto(int Points, bool IsDouble);

public sealed record StatisticDto(string Name, string Value);

public sealed record MatchCellDto(int? Points, int? RunningValue);

public sealed record MatchRowDto(int RoundNumber, IReadOnlyList<MatchCellDto> Cells);

public sealed record MatchColumnDto(int ParticipantId, string PlayerName, IReadOnlyList<StatisticDto> Statistics);

public sealed record ChartSeriesDto(
    string PlayerName,
    IReadOnlyList<double> RoundPoints);

public sealed record StandingDto(int Place, string PlayerName, int Total);

public sealed record MatchStateDto(
    int MatchId,
    string ModeTitle,
    string StatusTitle,
    bool IsInProgress,
    int CurrentRoundNumber,
    int? CurrentParticipantId,
    string? CurrentPlayerName,
    string? WinnerPlayerName,
    IReadOnlyList<MatchColumnDto> Columns,
    IReadOnlyList<MatchRowDto> Rows,
    IReadOnlyList<StatisticDto> MatchStatistics,
    bool ShowTrendChart,
    IReadOnlyList<ChartSeriesDto> ChartSeries,
    IReadOnlyList<StandingDto> Standings,
    int? CurrentRemaining);
