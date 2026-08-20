using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Matches.Rules;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence.Queries;

public sealed class MatchQueries(DartsDbContext context) : IMatchQueries
{
    public async Task<IReadOnlyList<MatchListItemDto>> ListAsync(
        MatchListFilter filter,
        CancellationToken cancellationToken)
    {
        var status = filter == MatchListFilter.InProgress ? MatchStatus.InProgress : MatchStatus.Finished;

        var raw = await context.Matches
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.StartedAt)
            .Select(m => new
            {
                m.Id,
                m.Mode,
                m.StartingScore,
                m.RoundLimit,
                m.StartedAt,
                m.FinishedAt,
                m.WinnerParticipantId,
                Participants = m.AllParticipants
                    .OrderBy(p => p.SeatOrder)
                    .Select(p => new { p.Id, Name = p.Player!.Name })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return raw
            .Select(m => new MatchListItemDto(
                m.Id,
                GameRules.TitleFor(m.Mode, m.StartingScore, m.RoundLimit),
                string.Join(", ", m.Participants.Select(p => p.Name)),
                m.Participants.FirstOrDefault(p => p.Id == m.WinnerParticipantId)?.Name,
                m.StartedAt,
                m.FinishedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<int>> GetLastParticipantPlayerIdsAsync(CancellationToken cancellationToken)
    {
        var lastMatchId = await context.Matches
            .OrderByDescending(m => m.StartedAt)
            .ThenByDescending(m => m.Id)
            .Select(m => (int?)m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastMatchId is null)
        {
            return Array.Empty<int>();
        }

        return await context.Matches
            .Where(m => m.Id == lastMatchId)
            .SelectMany(m => m.AllParticipants)
            .OrderBy(p => p.SeatOrder)
            .Select(p => p.PlayerId)
            .ToListAsync(cancellationToken);
    }
}
