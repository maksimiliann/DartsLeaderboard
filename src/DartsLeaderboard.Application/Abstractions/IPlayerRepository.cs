using DartsLeaderboard.Domain.Players;

namespace DartsLeaderboard.Application.Abstractions;

public interface IPlayerRepository
{
    Task<IReadOnlyList<Player>> ListAsync(bool includeArchived, CancellationToken cancellationToken);

    Task<Player?> GetAsync(int playerId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(string name, int? excludePlayerId, CancellationToken cancellationToken);

    Task AddAsync(Player player, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
