using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DartsLeaderboard.Infrastructure.Persistence.Repositories;

public sealed class MatchRepository(DartsDbContext context) : IMatchRepository
{
    private const string UniqueViolation = "23505";

    public Task<Match?> GetAsync(int matchId, CancellationToken cancellationToken) =>
        context.Matches
            .Include(m => m.AllParticipants)
            .ThenInclude(p => p.Player)
            .Include(m => m.AllThrows)
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

    public async Task AddAsync(Match match, CancellationToken cancellationToken) =>
        await context.Matches.AddAsync(match, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new MatchConflictException(exception);
        }
    }

    public async Task<bool> AllPlayersExistAsync(
        IReadOnlyList<int> playerIds,
        CancellationToken cancellationToken)
    {
        var found = await context.Players.CountAsync(p => playerIds.Contains(p.Id), cancellationToken);
        return found == playerIds.Distinct().Count();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };
}
