using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Abstractions;

public interface IRecordQueries
{
    Task<IReadOnlyList<ClubRecordDto>> GetAsync(CancellationToken cancellationToken);
}
