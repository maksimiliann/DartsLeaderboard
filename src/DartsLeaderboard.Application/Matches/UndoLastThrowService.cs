using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Matches;

public sealed class UndoLastThrowService(IMatchRepository matches, IMatchNotifier notifier)
{
    public async Task<OperationResult<MatchStateDto>> ExecuteAsync(
        int matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await matches.GetAsync(matchId, cancellationToken);
        if (match is null)
        {
            return OperationResult<MatchStateDto>.Fail(DomainErrorCode.MatchNotFound);
        }

        var undone = match.UndoLastThrow();
        if (!undone.IsSuccess)
        {
            return OperationResult<MatchStateDto>.Fail(undone.Error!.Value);
        }

        await matches.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged(matchId);

        return OperationResult<MatchStateDto>.Success(MatchStateMapper.ToDto(match));
    }
}
