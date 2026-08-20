using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class StubLeaderboardQueries : ILeaderboardQueries
{
    public Task<IReadOnlyList<LeaderboardRowDto>> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LeaderboardRowDto>>(Array.Empty<LeaderboardRowDto>());
}

internal sealed class StubRecordQueries : IRecordQueries
{
    public Task<IReadOnlyList<ClubRecordDto>> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ClubRecordDto>>(Array.Empty<ClubRecordDto>());
}

internal class StubWinArchiveQueries : IWinArchiveQueries
{
    public Task<IReadOnlyList<WinArchiveRowDto>> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WinArchiveRowDto>>(Array.Empty<WinArchiveRowDto>());

    public virtual Task SaveAsync(IReadOnlyList<WinArchiveRowDto> rows, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

internal sealed class StubMatchQueries : IMatchQueries
{
    public Task<IReadOnlyList<MatchListItemDto>> ListAsync(MatchListFilter filter, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MatchListItemDto>>(Array.Empty<MatchListItemDto>());

    public Task<IReadOnlyList<int>> GetLastParticipantPlayerIdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
}
