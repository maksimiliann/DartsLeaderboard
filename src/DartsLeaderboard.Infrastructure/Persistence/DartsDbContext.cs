using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Players;
using DartsLeaderboard.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence;

public sealed class DartsDbContext(DbContextOptions<DartsDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<PlayerWinArchive> WinArchives => Set<PlayerWinArchive>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DartsDbContext).Assembly);
}
