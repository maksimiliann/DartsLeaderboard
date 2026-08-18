using System.Reflection;
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Domain.Players;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class FakePlayerRepository : IPlayerRepository
{
    private readonly List<Player> _players = new();
    private int _nextId;

    public int SaveCount { get; private set; }

    public Player Seed(string name, bool archived = false)
    {
        var player = Player.Create(name, new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero)).Value!;
        AssignId(player);
        player.SetArchived(archived);
        _players.Add(player);
        return player;
    }

    public Task<IReadOnlyList<Player>> ListAsync(bool includeArchived, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Player>>(_players
            .Where(p => includeArchived || !p.IsArchived)
            .OrderBy(p => p.Name)
            .ToList());

    public Task<Player?> GetAsync(int playerId, CancellationToken cancellationToken) =>
        Task.FromResult(_players.FirstOrDefault(p => p.Id == playerId));

    public Task<bool> ExistsWithNameAsync(string name, int? excludePlayerId, CancellationToken cancellationToken) =>
        Task.FromResult(_players.Any(p =>
            p.Id != excludePlayerId &&
            string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(Player player, CancellationToken cancellationToken)
    {
        AssignId(player);
        _players.Add(player);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    private void AssignId(Player player) =>
        typeof(Player)
            .GetProperty(nameof(Player.Id), BindingFlags.Public | BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(player, new object[] { ++_nextId });
}
