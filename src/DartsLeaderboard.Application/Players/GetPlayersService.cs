using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Players;

public sealed class GetPlayersService(IPlayerRepository repository)
{
    public async Task<IReadOnlyList<PlayerDto>> ExecuteAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var players = await repository.ListAsync(includeArchived, cancellationToken);
        return players.Select(p => new PlayerDto(p.Id, p.Name, p.IsArchived)).ToList();
    }
}
