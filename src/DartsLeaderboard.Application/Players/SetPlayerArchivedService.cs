using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Players;

public sealed class SetPlayerArchivedService(IPlayerRepository repository)
{
    public async Task<OperationResult> ExecuteAsync(
        int playerId,
        bool archived,
        CancellationToken cancellationToken = default)
    {
        var player = await repository.GetAsync(playerId, cancellationToken);
        if (player is null)
        {
            return OperationResult.Fail(DomainErrorCode.PlayerNotFound);
        }

        player.SetArchived(archived);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
