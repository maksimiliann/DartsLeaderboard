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
}
