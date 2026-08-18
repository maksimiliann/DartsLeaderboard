using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class FakeMatchRepository : IMatchRepository
{
    private readonly Dictionary<int, Match> _matches = new();

    public bool ThrowConflictOnSave { get; set; }

    public int SaveCount { get; private set; }

    public void Seed(Match match) => _matches[match.Id] = match;

    public Task<Match?> GetAsync(int matchId, CancellationToken cancellationToken) =>
        Task.FromResult(_matches.TryGetValue(matchId, out var match) ? match : null);

    public Task AddAsync(Match match, CancellationToken cancellationToken)
    {
        _matches[match.Id] = match;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (ThrowConflictOnSave)
        {
            throw new MatchConflictException();
        }

        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<bool> AllPlayersExistAsync(IReadOnlyList<int> playerIds, CancellationToken cancellationToken) =>
        Task.FromResult(MissingPlayerIds.Intersect(playerIds).Any() == false);

    public IReadOnlyCollection<int> MissingPlayerIds { get; set; } = Array.Empty<int>();
}
