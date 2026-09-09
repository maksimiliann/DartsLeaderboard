using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Reports;
using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence.Queries;

public sealed class WeeklyHighlightQueries(DartsDbContext context) : IWeeklyHighlightQueries
{
    public async Task<IReadOnlyList<WeeklyHighlightDto>> GetAsync(
        DateTimeOffset weekStart,
        DateTimeOffset weekEnd,
        CancellationToken cancellationToken)
    {
        var matches = await context.Matches
            .Where(match =>
                match.Status == MatchStatus.Finished &&
                match.FinishedAt != null &&
                match.FinishedAt >= weekStart.ToUniversalTime() &&
                match.FinishedAt < weekEnd.ToUniversalTime())
            .Select(match => new
            {
                match.Mode,
                match.StartedAt,
                FinishedAt = match.FinishedAt!.Value,
                match.WinnerParticipantId,
                Participants = match.AllParticipants
                    .OrderBy(participant => participant.SeatOrder)
                    .Select(participant => new
                    {
                        participant.Id,
                        participant.PlayerId,
                        Name = participant.Player!.Name
                    })
                    .ToList(),
                Throws = match.AllThrows
                    .Select(recorded => new
                    {
                        recorded.ParticipantId,
                        recorded.Points,
                        recorded.RecordedAt
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var players = new Dictionary<int, PlayerAcc>();
        var matchFacts = new List<WeeklyMatchFacts>(matches.Count);

        foreach (var match in matches)
        {
            matchFacts.Add(new WeeklyMatchFacts(
                string.Join(", ", match.Participants.Select(participant => participant.Name)),
                match.FinishedAt - match.StartedAt,
                match.FinishedAt));

            foreach (var participant in match.Participants)
            {
                var acc = GetOrAdd(players, participant.PlayerId, participant.Name);
                var visits = match.Throws
                    .Where(visit => visit.ParticipantId == participant.Id)
                    .ToList();

                if (match.WinnerParticipantId == participant.Id)
                {
                    acc.Wins++;
                    acc.WinsAt = match.FinishedAt;
                    if (match.Mode == GameMode.X01)
                    {
                        acc.WinsX01++;
                        acc.WinsX01At = match.FinishedAt;
                        var rounds = visits.Count;
                        if (acc.FastestX01Rounds is null
                            || rounds < acc.FastestX01Rounds
                            || (rounds == acc.FastestX01Rounds && match.FinishedAt < acc.FastestX01At))
                        {
                            acc.FastestX01Rounds = rounds;
                            acc.FastestX01At = match.FinishedAt;
                        }

                        if (acc.SlowestX01Rounds is null
                            || rounds > acc.SlowestX01Rounds
                            || (rounds == acc.SlowestX01Rounds && match.FinishedAt < acc.SlowestX01At))
                        {
                            acc.SlowestX01Rounds = rounds;
                            acc.SlowestX01At = match.FinishedAt;
                        }
                    }
                    else if (match.Mode == GameMode.HighestTotal)
                    {
                        acc.WinsHighestTotal++;
                        acc.WinsHighestTotalAt = match.FinishedAt;
                    }
                }
                else if (match.WinnerParticipantId is not null)
                {
                    acc.Losses++;
                    acc.LossesAt = match.FinishedAt;
                }

                foreach (var visit in visits)
                {
                    if (visit.Points == 26)
                    {
                        acc.Count26++;
                        acc.Count26At = visit.RecordedAt;
                    }

                    if (visit.Points == 21)
                    {
                        acc.Count21++;
                        acc.Count21At = visit.RecordedAt;
                    }
                }

                if (match.Mode != GameMode.HighestTotal || visits.Count == 0)
                {
                    continue;
                }

                acc.HighestTotalSum = (acc.HighestTotalSum ?? 0) + visits.Sum(visit => visit.Points);
                acc.HighestTotalSumAt = visits.Max(visit => visit.RecordedAt);

                foreach (var visit in visits)
                {
                    if (acc.BestVisit is null
                        || visit.Points > acc.BestVisit
                        || (visit.Points == acc.BestVisit && visit.RecordedAt < acc.BestVisitAt))
                    {
                        acc.BestVisit = visit.Points;
                        acc.BestVisitAt = visit.RecordedAt;
                    }

                    if (acc.WorstVisit is null
                        || visit.Points < acc.WorstVisit
                        || (visit.Points == acc.WorstVisit && visit.RecordedAt < acc.WorstVisitAt))
                    {
                        acc.WorstVisit = visit.Points;
                        acc.WorstVisitAt = visit.RecordedAt;
                    }
                }
            }
        }

        return WeeklyHighlights.From(players.Values.Select(acc => acc.ToFacts()).ToList(), matchFacts);
    }

    private static PlayerAcc GetOrAdd(Dictionary<int, PlayerAcc> players, int playerId, string name)
    {
        if (!players.TryGetValue(playerId, out var acc))
        {
            acc = new PlayerAcc { Name = name };
            players[playerId] = acc;
        }

        return acc;
    }

    private sealed class PlayerAcc
    {
        public required string Name { get; init; }

        public int Wins { get; set; }

        public DateTimeOffset WinsAt { get; set; }

        public int Losses { get; set; }

        public DateTimeOffset LossesAt { get; set; }

        public int WinsX01 { get; set; }

        public DateTimeOffset WinsX01At { get; set; }

        public int WinsHighestTotal { get; set; }

        public DateTimeOffset WinsHighestTotalAt { get; set; }

        public int? BestVisit { get; set; }

        public DateTimeOffset? BestVisitAt { get; set; }

        public int? WorstVisit { get; set; }

        public DateTimeOffset? WorstVisitAt { get; set; }

        public int? HighestTotalSum { get; set; }

        public DateTimeOffset? HighestTotalSumAt { get; set; }

        public int Count26 { get; set; }

        public DateTimeOffset Count26At { get; set; }

        public int Count21 { get; set; }

        public DateTimeOffset Count21At { get; set; }

        public int? FastestX01Rounds { get; set; }

        public DateTimeOffset FastestX01At { get; set; }

        public int? SlowestX01Rounds { get; set; }

        public DateTimeOffset SlowestX01At { get; set; }

        public WeeklyPlayerFacts ToFacts() =>
            new(
                Name,
                Wins,
                WinsAt,
                Losses,
                LossesAt,
                WinsX01,
                WinsX01At,
                WinsHighestTotal,
                WinsHighestTotalAt,
                BestVisit,
                BestVisitAt,
                WorstVisit,
                WorstVisitAt,
                HighestTotalSum,
                HighestTotalSumAt,
                Count26,
                Count26At,
                Count21,
                Count21At,
                FastestX01Rounds,
                FastestX01At,
                SlowestX01Rounds,
                SlowestX01At);
    }
}
