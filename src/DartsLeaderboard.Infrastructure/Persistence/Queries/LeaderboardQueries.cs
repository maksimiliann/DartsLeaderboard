using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence.Queries;

public sealed class LeaderboardQueries(DartsDbContext context) : ILeaderboardQueries
{
    public async Task<IReadOnlyList<LeaderboardRowDto>> GetAsync(CancellationToken cancellationToken)
    {
        var rows = await context.Players
            .Select(player => new
            {
                player.Id,
                player.Name,
                LiveX01 = context.Matches.Count(m =>
                    m.Status == MatchStatus.Finished &&
                    m.Mode == GameMode.X01 &&
                    m.AllParticipants.Any(p => p.PlayerId == player.Id && p.Id == m.WinnerParticipantId)),
                LiveHighestTotal = context.Matches.Count(m =>
                    m.Status == MatchStatus.Finished &&
                    m.Mode == GameMode.HighestTotal &&
                    m.AllParticipants.Any(p => p.PlayerId == player.Id && p.Id == m.WinnerParticipantId)),
                MatchesPlayed = context.Matches.Count(m =>
                    m.Status == MatchStatus.Finished &&
                    m.AllParticipants.Any(p => p.PlayerId == player.Id)),
                ArchiveX01 = context.WinArchives
                    .Where(archive => archive.PlayerId == player.Id)
                    .Select(archive => archive.WinsX01)
                    .FirstOrDefault(),
                ArchiveHighestTotal = context.WinArchives
                    .Where(archive => archive.PlayerId == player.Id)
                    .Select(archive => archive.WinsHighestTotal)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new LeaderboardRowDto(
                row.Id,
                row.Name,
                row.LiveX01 + row.ArchiveX01,
                row.LiveHighestTotal + row.ArchiveHighestTotal,
                row.MatchesPlayed,
                row.LiveX01 + row.LiveHighestTotal))
            .OrderByDescending(r => r.Wins)
            .ThenByDescending(r => r.MatchesPlayed)
            .ThenBy(r => r.PlayerName)
            .ToList();
    }
}
