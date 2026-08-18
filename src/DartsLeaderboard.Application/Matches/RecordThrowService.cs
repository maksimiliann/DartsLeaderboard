using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Matches;

public sealed class RecordThrowService(IMatchRepository matches, IClock clock, IMatchNotifier notifier)
{
    public async Task<OperationResult<MatchStateDto>> ExecuteAsync(
        int matchId,
        int points,
        CancellationToken cancellationToken = default)
    {
        var match = await matches.GetAsync(matchId, cancellationToken);
        if (match is null)
        {
            return OperationResult<MatchStateDto>.Fail(DomainErrorCode.MatchNotFound);
        }

        var recorded = match.RecordThrow(points, clock.UtcNow);
        if (!recorded.IsSuccess)
        {
            return OperationResult<MatchStateDto>.Fail(recorded.Error!.Value);
        }

        try
        {
            await matches.SaveChangesAsync(cancellationToken);
        }
        catch (MatchConflictException)
        {
            return OperationResult<MatchStateDto>.Fail(DomainErrorCode.RoundAlreadyRecorded);
        }

        notifier.NotifyChanged(matchId);
        return OperationResult<MatchStateDto>.Success(MatchStateMapper.ToDto(match));
    }
}
