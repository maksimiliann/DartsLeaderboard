using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Domain.Players;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence.Repositories;

public sealed class PlayerRepository(DartsDbContext context) : IPlayerRepository
{
    public async Task<IReadOnlyList<Player>> ListAsync(bool includeArchived, CancellationToken cancellationToken) =>
        await context.Players
            .Where(p => includeArchived || !p.IsArchived)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<Player?> GetAsync(int playerId, CancellationToken cancellationToken) =>
        context.Players.FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);

    public Task<bool> ExistsWithNameAsync(string name, int? excludePlayerId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return context.Players.AnyAsync(
            p => p.Name.ToLower() == normalized && (excludePlayerId == null || p.Id != excludePlayerId),
            cancellationToken);
    }

    public async Task AddAsync(Player player, CancellationToken cancellationToken) =>
        await context.Players.AddAsync(player, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
