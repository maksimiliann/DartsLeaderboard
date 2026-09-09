using Bunit;
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Reports;
using DartsLeaderboard.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace DartsLeaderboard.Web.Tests;

public class LeaderboardTests : BunitContext, IAsyncLifetime
{
    public LeaderboardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new GetLeaderboardService(new EmptyLeaderboardQueries()));
        Services.AddSingleton(new GetRecordsService(new EmptyRecordQueries()));
        Services.AddSingleton(new GetMatchListService(new EmptyMatchQueries()));
        Services.AddSingleton<IClock, LeaderboardTestClock>();
        Services.AddSingleton<IWeeklyHighlightQueries, EmptyWeeklyHighlightQueries>();
        Services.AddSingleton<GetWeeklyHighlightsService>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    [Fact]
    public void ShowsWeekHighlightsButton()
    {
        var page = Render<Leaderboard>();

        var button = page.Find("[data-testid=week-highlights]");
        Assert.Contains("Итоги недели", button.TextContent, StringComparison.Ordinal);
    }

    private sealed class EmptyLeaderboardQueries : ILeaderboardQueries
    {
        public Task<IReadOnlyList<LeaderboardRowDto>> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LeaderboardRowDto>>(Array.Empty<LeaderboardRowDto>());
    }

    private sealed class EmptyRecordQueries : IRecordQueries
    {
        public Task<IReadOnlyList<ClubRecordDto>> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ClubRecordDto>>(Array.Empty<ClubRecordDto>());
    }

    private sealed class EmptyMatchQueries : IMatchQueries
    {
        public Task<IReadOnlyList<MatchListItemDto>> ListAsync(
            MatchListFilter filter,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MatchListItemDto>>(Array.Empty<MatchListItemDto>());

        public Task<IReadOnlyList<int>> GetLastParticipantPlayerIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }

    private sealed class LeaderboardTestClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class EmptyWeeklyHighlightQueries : IWeeklyHighlightQueries
    {
        public Task<IReadOnlyList<WeeklyHighlightDto>> GetAsync(
            DateTimeOffset weekStart,
            DateTimeOffset weekEnd,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WeeklyHighlightDto>>(WeeklyHighlights.From([], []));
    }
}
