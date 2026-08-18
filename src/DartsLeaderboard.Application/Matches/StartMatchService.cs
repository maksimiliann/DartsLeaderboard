using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Matches;

public sealed class StartMatchService(IMatchRepository matches, IClock clock)
{
    public async Task<OperationResult<int>> ExecuteAsync(
        MatchSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = request.Mode switch
        {
            GameModeOption.X01 => MatchSettings.X01(request.StartingScore ?? 0),
            GameModeOption.HighestTotal => MatchSettings.HighestTotal(request.RoundLimit ?? 0),
            _ => null
        };

        if (settings is null)
        {
            return OperationResult<int>.Fail("Неизвестный режим игры");
        }

        var started = Match.Start(settings, request.PlayerIds, clock.UtcNow);
        if (!started.IsSuccess)
        {
            return OperationResult<int>.Fail(started.Error!.Value);
        }

        if (!await matches.AllPlayersExistAsync(request.PlayerIds, cancellationToken))
        {
            return OperationResult<int>.Fail(DomainErrorCode.PlayerNotFound);
        }

        await matches.AddAsync(started.Value!, cancellationToken);
        await matches.SaveChangesAsync(cancellationToken);

        return OperationResult<int>.Success(started.Value!.Id);
    }
}
