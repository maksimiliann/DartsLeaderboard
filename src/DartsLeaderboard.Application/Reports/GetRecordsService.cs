using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Reports;

public sealed class GetRecordsService(IRecordQueries queries)
{
    public Task<IReadOnlyList<ClubRecordDto>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        queries.GetAsync(cancellationToken);
}
