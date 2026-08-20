using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Infrastructure.Notifications;
using DartsLeaderboard.Infrastructure.Persistence;
using DartsLeaderboard.Infrastructure.Persistence.Queries;
using DartsLeaderboard.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DartsLeaderboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DartsDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ILeaderboardQueries, LeaderboardQueries>();
        services.AddScoped<IMatchQueries, MatchQueries>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMatchNotifier, InMemoryMatchNotifier>();
        services.AddHostedService<DatabaseInitializer>();

        return services;
    }
}
