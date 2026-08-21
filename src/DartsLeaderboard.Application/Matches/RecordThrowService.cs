using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Matches;

public sealed class RecordThrowService(IMatchRepository matches, IClock clock, IMatchNotifier notifier)
{
    public Task<OperationResult<MatchStateDto>> ExecuteAsync(
        int matchId,
        int points,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(matchId, [new ThrowDartDto(points, false)], cancellationToken);

    public async Task<OperationResult<MatchStateDto>> ExecuteAsync(
        int matchId,
        IReadOnlyList<ThrowDartDto> darts,
        CancellationToken cancellationToken = default)
    {
        var match = await matches.GetAsync(matchId, cancellationToken);
        if (match is null)
        {
            return OperationResult<MatchStateDto>.Fail(DomainErrorCode.MatchNotFound);
        }

        var visit = darts.Select(dart => new VisitDart(dart.Points, dart.IsDouble)).ToList();
        var recorded = match.RecordThrow(visit, clock.UtcNow);
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
