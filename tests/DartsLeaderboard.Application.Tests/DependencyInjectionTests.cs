using DartsLeaderboard.Application;
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Matches;
using DartsLeaderboard.Application.Players;
using DartsLeaderboard.Application.Reports;
using DartsLeaderboard.Application.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace DartsLeaderboard.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ResolvesEveryUseCase()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IPlayerRepository, FakePlayerRepository>();
        services.AddSingleton<IMatchRepository, FakeMatchRepository>();
        services.AddSingleton<IMatchNotifier, RecordingNotifier>();
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<ILeaderboardQueries, StubLeaderboardQueries>();
        services.AddSingleton<IRecordQueries, StubRecordQueries>();
        services.AddSingleton<IMatchQueries, StubMatchQueries>();
        services.AddSingleton<IWinArchiveQueries, StubWinArchiveQueries>();
        services.AddSingleton<IWeeklyHighlightQueries, StubWeeklyHighlightQueries>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AddPlayerService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetPlayersService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RenamePlayerService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<SetPlayerArchivedService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<StartMatchService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RecordThrowService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<UndoLastThrowService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AbandonMatchService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetMatchStateService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetLeaderboardService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetRecordsService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetMatchListService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetWinArchiveService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<SaveWinArchiveService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetWeeklyHighlightsService>());
    }
}
