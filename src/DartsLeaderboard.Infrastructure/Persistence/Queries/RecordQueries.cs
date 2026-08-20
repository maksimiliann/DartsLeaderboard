using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Reports;
using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence.Queries;

public sealed class RecordQueries(DartsDbContext context) : IRecordQueries
{
    public async Task<IReadOnlyList<ClubRecordDto>> GetAsync(CancellationToken cancellationToken)
    {
        var throwRows = await context.Matches
            .Where(match => match.Status == MatchStatus.Finished)
            .SelectMany(match => match.AllThrows.Select(recorded => new
            {
                Name = match.AllParticipants.First(p => p.Id == recorded.ParticipantId).Player!.Name,
                recorded.Points,
                recorded.RecordedAt
            }))
            .ToListAsync(cancellationToken);

        var fiveRows = await context.Matches
            .Where(match =>
                match.Status == MatchStatus.Finished &&
                match.Mode == GameMode.HighestTotal &&
                match.RoundLimit == 5)
            .SelectMany(match => match.AllParticipants.Select(participant => new
            {
                Name = participant.Player!.Name,
                Total = match.AllThrows
                    .Where(recorded => recorded.ParticipantId == participant.Id)
                    .Sum(recorded => recorded.Points),
                Count = match.AllThrows.Count(recorded => recorded.ParticipantId == participant.Id),
                match.FinishedAt
            }))
            .Where(row => row.Count == 5 && row.FinishedAt != null)
            .ToListAsync(cancellationToken);

        return ClubRecords.From(
            throwRows.Select(row => new RecordCandidate(row.Name, row.Points, row.RecordedAt)).ToList(),
            fiveRows.Select(row => new RecordCandidate(row.Name, row.Total, row.FinishedAt!.Value)).ToList());
    }
}
