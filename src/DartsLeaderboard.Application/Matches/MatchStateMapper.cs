using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Matches.Rules;

namespace DartsLeaderboard.Application.Matches;

public static class MatchStateMapper
{
    public static MatchStateDto ToDto(Match match)
    {
        var rules = match.Rules;
        var participants = match.Participants;

        var rowCount = match.Status == MatchStatus.InProgress
            ? Math.Max(match.CompletedRoundCount, match.CurrentRoundNumber)
            : match.CompletedRoundCount;

        var throwsByCell = match.Throws.ToDictionary(t => (t.ParticipantId, t.RoundNumber));

        var rows = new List<MatchRowDto>();
        for (var round = 1; round <= rowCount; round++)
        {
            var cells = participants
                .Select(p => throwsByCell.TryGetValue((p.Id, round), out var recorded)
                    ? new MatchCellDto(recorded.Points, null)
                    : new MatchCellDto(null, null))
                .ToList();

            rows.Add(new MatchRowDto(round, cells));
        }

        var columns = participants
            .Select(p => new MatchColumnDto(p.Id, p.PlayerName, Map(rules.BuildPlayerStatistics(match, p))))
            .ToList();

        var winner = participants.FirstOrDefault(p => p.Id == match.WinnerParticipantId);

        return new MatchStateDto(
            match.Id,
            rules.Title,
            StatusTitle(match),
            match.Status == MatchStatus.InProgress,
            match.CurrentRoundNumber,
            match.CurrentParticipant?.Id,
            match.CurrentParticipant?.PlayerName,
            winner?.PlayerName,
            columns,
            rows,
            Map(rules.BuildMatchStatistics(match)),
            rules.SupportsTrendChart,
            rules.SupportsTrendChart ? BuildSeries(match) : Array.Empty<ChartSeriesDto>(),
            rules.BuildStandings(match).Select(s => new StandingDto(s.Place, s.PlayerName, s.Total)).ToList(),
            CurrentRemaining(match));
    }

    private static int? CurrentRemaining(Match match)
    {
        if (match.Mode != GameMode.X01 || match.CurrentParticipant is null)
        {
            return null;
        }

        return match.StartingScore!.Value - match.PointsOf(match.CurrentParticipant.Id);
    }

    private static IReadOnlyList<StatisticDto> Map(IReadOnlyList<StatisticItem> items) =>
        items.Select(i => new StatisticDto(i.Name, i.Value)).ToList();

    private static string StatusTitle(Match match) => match.Status switch
    {
        MatchStatus.InProgress => "Идёт",
        MatchStatus.Abandoned => "Прерван",
        MatchStatus.Finished when match.WinnerParticipantId is null => "Ничья",
        _ => "Завершён"
    };

    private static IReadOnlyList<ChartSeriesDto> BuildSeries(Match match) =>
        match.Participants
            .Select(p =>
            {
                var points = match.Throws
                    .Where(t => t.ParticipantId == p.Id)
                    .OrderBy(t => t.RoundNumber)
                    .Select(t => (double)t.Points)
                    .ToList();

                var cumulative = new List<double>(points.Count);
                var running = 0d;
                foreach (var value in points)
                {
                    running += value;
                    cumulative.Add(running);
                }

                return new ChartSeriesDto(p.PlayerName, points, cumulative);
            })
            .ToList();
}
