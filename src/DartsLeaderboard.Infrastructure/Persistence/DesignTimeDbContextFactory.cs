using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DartsLeaderboard.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DartsDbContext>
{
    public DartsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<DartsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=darts;Username=darts;Password=darts")
            .Options);
}
