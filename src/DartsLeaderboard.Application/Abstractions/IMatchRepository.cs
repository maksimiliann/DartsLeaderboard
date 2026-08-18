using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Abstractions;

public interface IMatchRepository
{
    Task<Match?> GetAsync(int matchId, CancellationToken cancellationToken);

    Task AddAsync(Match match, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> AllPlayersExistAsync(IReadOnlyList<int> playerIds, CancellationToken cancellationToken);
}
