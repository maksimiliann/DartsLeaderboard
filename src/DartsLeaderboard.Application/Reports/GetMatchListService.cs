using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Reports;

public sealed class GetMatchListService(IMatchQueries queries)
{
    public Task<IReadOnlyList<MatchListItemDto>> ExecuteAsync(
        MatchListFilter filter,
        CancellationToken cancellationToken = default) =>
        queries.ListAsync(filter, cancellationToken);
}
