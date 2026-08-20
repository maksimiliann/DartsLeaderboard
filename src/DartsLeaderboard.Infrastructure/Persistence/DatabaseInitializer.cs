using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DartsLeaderboard.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(3);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DartsDbContext>();
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Миграции применены с попытки {Attempt}", attempt);
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts)
            {
                logger.LogWarning(exception, "База недоступна, попытка {Attempt} из {Max}", attempt, MaxAttempts);
                await Task.Delay(Delay, cancellationToken);
            }
        }

        throw new InvalidOperationException("Не удалось применить миграции: база недоступна");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
