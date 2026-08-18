using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Matches;

public sealed class GetMatchStateService(IMatchRepository matches)
{
    public async Task<OperationResult<MatchStateDto>> ExecuteAsync(
        int matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await matches.GetAsync(matchId, cancellationToken);
        return match is null
            ? OperationResult<MatchStateDto>.Fail(DomainErrorCode.MatchNotFound)
            : OperationResult<MatchStateDto>.Success(MatchStateMapper.ToDto(match));
    }
}
