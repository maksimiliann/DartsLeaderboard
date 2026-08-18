using DartsLeaderboard.Application.Matches;
using DartsLeaderboard.Application.Players;
using DartsLeaderboard.Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace DartsLeaderboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetPlayersService>();
        services.AddScoped<AddPlayerService>();
        services.AddScoped<RenamePlayerService>();
        services.AddScoped<SetPlayerArchivedService>();

        services.AddScoped<StartMatchService>();
        services.AddScoped<GetMatchStateService>();
        services.AddScoped<RecordThrowService>();
        services.AddScoped<UndoLastThrowService>();
        services.AddScoped<AbandonMatchService>();

        services.AddScoped<GetLeaderboardService>();
        services.AddScoped<GetMatchListService>();

        return services;
    }
}
