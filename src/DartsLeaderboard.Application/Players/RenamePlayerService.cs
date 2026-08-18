using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Players;

public sealed class RenamePlayerService(IPlayerRepository repository)
{
    public async Task<OperationResult> ExecuteAsync(
        int playerId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var player = await repository.GetAsync(playerId, cancellationToken);
        if (player is null)
        {
            return OperationResult.Fail(DomainErrorCode.PlayerNotFound);
        }

        if (await repository.ExistsWithNameAsync(name, playerId, cancellationToken))
        {
            return OperationResult.Fail(DomainErrorCode.PlayerNameTaken);
        }

        var renamed = player.Rename(name);
        if (!renamed.IsSuccess)
        {
            return OperationResult.Fail(renamed.Error!.Value);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
