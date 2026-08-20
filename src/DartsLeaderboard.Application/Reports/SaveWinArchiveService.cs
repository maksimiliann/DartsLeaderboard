using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Reports;

public sealed class SaveWinArchiveService(IWinArchiveQueries queries)
{
    public async Task<OperationResult> ExecuteAsync(
        IReadOnlyList<WinArchiveRowDto> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Any(row => row.WinsX01 < 0 || row.WinsHighestTotal < 0))
        {
            return OperationResult.Fail(DomainErrorCode.InvalidWinArchiveCount);
        }

        await queries.SaveAsync(rows, cancellationToken);
        return OperationResult.Success();
    }
}
