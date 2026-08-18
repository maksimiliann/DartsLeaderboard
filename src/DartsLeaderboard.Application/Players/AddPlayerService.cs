using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Players;

namespace DartsLeaderboard.Application.Players;

public sealed class AddPlayerService(IPlayerRepository repository, IClock clock)
{
    public async Task<OperationResult<PlayerDto>> ExecuteAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var created = Player.Create(name, clock.UtcNow);
        if (!created.IsSuccess)
        {
            return OperationResult<PlayerDto>.Fail(created.Error!.Value);
        }

        if (await repository.ExistsWithNameAsync(created.Value!.Name, null, cancellationToken))
        {
            return OperationResult<PlayerDto>.Fail(DomainErrorCode.PlayerNameTaken);
        }

        await repository.AddAsync(created.Value, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var player = created.Value;
        return OperationResult<PlayerDto>.Success(new PlayerDto(player.Id, player.Name, player.IsArchived));
    }
}
