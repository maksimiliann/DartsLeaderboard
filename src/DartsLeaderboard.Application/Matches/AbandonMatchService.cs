using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Matches;

public sealed class AbandonMatchService(IMatchRepository matches, IClock clock, IMatchNotifier notifier)
{
    public async Task<OperationResult> ExecuteAsync(int matchId, CancellationToken cancellationToken = default)
    {
        var match = await matches.GetAsync(matchId, cancellationToken);
        if (match is null)
        {
            return OperationResult.Fail(DomainErrorCode.MatchNotFound);
        }

        var abandoned = match.Abandon(clock.UtcNow);
        if (!abandoned.IsSuccess)
        {
            return OperationResult.Fail(abandoned.Error!.Value);
        }

        await matches.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged(matchId);

        return OperationResult.Success();
    }
}
