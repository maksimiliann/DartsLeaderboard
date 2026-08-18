using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class StubLeaderboardQueries : ILeaderboardQueries
{
    public Task<IReadOnlyList<LeaderboardRowDto>> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LeaderboardRowDto>>(Array.Empty<LeaderboardRowDto>());
}

internal sealed class StubMatchQueries : IMatchQueries
{
    public Task<IReadOnlyList<MatchListItemDto>> ListAsync(MatchListFilter filter, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MatchListItemDto>>(Array.Empty<MatchListItemDto>());
}
