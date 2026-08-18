using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Abstractions;

public interface ILeaderboardQueries
{
    Task<IReadOnlyList<LeaderboardRowDto>> GetAsync(CancellationToken cancellationToken);
}
