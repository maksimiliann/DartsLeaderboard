using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Abstractions;

public interface IWinArchiveQueries
{
    Task<IReadOnlyList<WinArchiveRowDto>> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(IReadOnlyList<WinArchiveRowDto> rows, CancellationToken cancellationToken);
}
