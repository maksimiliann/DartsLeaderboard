using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Reports;

public sealed class GetWinArchiveService(IWinArchiveQueries queries)
{
    public Task<IReadOnlyList<WinArchiveRowDto>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        queries.GetAsync(cancellationToken);
}
