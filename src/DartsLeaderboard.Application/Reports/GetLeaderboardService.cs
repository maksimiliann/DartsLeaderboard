using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Reports;

public sealed class GetLeaderboardService(ILeaderboardQueries queries)
{
    public Task<IReadOnlyList<LeaderboardRowDto>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        queries.GetAsync(cancellationToken);
}
