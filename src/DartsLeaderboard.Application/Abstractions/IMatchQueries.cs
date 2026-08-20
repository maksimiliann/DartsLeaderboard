using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Abstractions;

public interface IMatchQueries
{
    Task<IReadOnlyList<MatchListItemDto>> ListAsync(MatchListFilter filter, CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> GetLastParticipantPlayerIdsAsync(CancellationToken cancellationToken);
}
