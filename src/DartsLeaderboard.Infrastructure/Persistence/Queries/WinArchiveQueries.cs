using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence.Queries;

public sealed class WinArchiveQueries(DartsDbContext context) : IWinArchiveQueries
{
    public async Task<IReadOnlyList<WinArchiveRowDto>> GetAsync(CancellationToken cancellationToken)
    {
        return await context.Players
            .OrderBy(player => player.Name)
            .Select(player => new WinArchiveRowDto(
                player.Id,
                player.Name,
                context.WinArchives
                    .Where(archive => archive.PlayerId == player.Id)
                    .Select(archive => archive.WinsX01)
                    .FirstOrDefault(),
                context.WinArchives
                    .Where(archive => archive.PlayerId == player.Id)
                    .Select(archive => archive.WinsHighestTotal)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(IReadOnlyList<WinArchiveRowDto> rows, CancellationToken cancellationToken)
    {
        var existing = await context.WinArchives.ToListAsync(cancellationToken);
        var byPlayer = existing.ToDictionary(archive => archive.PlayerId);

        foreach (var row in rows)
        {
            if (row.WinsX01 == 0 && row.WinsHighestTotal == 0)
            {
                if (byPlayer.Remove(row.PlayerId, out var empty))
                {
                    context.WinArchives.Remove(empty);
                }

                continue;
            }

            if (byPlayer.TryGetValue(row.PlayerId, out var archive))
            {
                archive.WinsX01 = row.WinsX01;
                archive.WinsHighestTotal = row.WinsHighestTotal;
                continue;
            }

            context.WinArchives.Add(new PlayerWinArchive
            {
                PlayerId = row.PlayerId,
                WinsX01 = row.WinsX01,
                WinsHighestTotal = row.WinsHighestTotal
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
