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
            .Select(player => new LeaderboardRowDto(
                player.Id,
                player.Name,
                context.Matches.Count(m =>
                    m.Status == MatchStatus.Finished &&
                    m.AllParticipants.Any(p => p.PlayerId == player.Id && p.Id == m.WinnerParticipantId)),
                context.Matches.Count(m =>
                    m.Status == MatchStatus.Finished &&
                    m.AllParticipants.Any(p => p.PlayerId == player.Id))))
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(r => r.Wins)
            .ThenByDescending(r => r.MatchesPlayed)
            .ThenBy(r => r.PlayerName)
            .ToList();
    }
}
