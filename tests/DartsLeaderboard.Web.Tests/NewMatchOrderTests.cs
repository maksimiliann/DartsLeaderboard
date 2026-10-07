using System.Reflection;
using Bunit;
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Matches;
using DartsLeaderboard.Application.Players;
using DartsLeaderboard.Application.Reports;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Players;
using DartsLeaderboard.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace DartsLeaderboard.Web.Tests;

public class NewMatchOrderTests : BunitContext, IAsyncLifetime
{
    private readonly StubPlayerRepository _players = new();
    private readonly StubMatchQueries _queries = new();

    public NewMatchOrderTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IPlayerRepository>(_players);
        Services.AddSingleton<GetPlayersService>();
        Services.AddSingleton<IMatchQueries>(_queries);
        Services.AddSingleton<GetMatchListService>();
        Services.AddSingleton<IMatchRepository, UnusedMatchRepository>();
        Services.AddSingleton<IClock, FixedClock>();
        Services.AddSingleton<StartMatchService>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    [Fact]
    public void ChangingTurnOrder_RealignsPlayerList()
    {
        var anya = _players.Seed("Аня");
        _players.Seed("Боря");
        var vasya = _players.Seed("Вася");
        _queries.LastPlayerIds = [vasya.Id, anya.Id];

        var page = Render<NewMatch>();

        Assert.Equal(["Вася", "Аня", "Боря"], Names(page));

        page.FindAll("button[aria-label=Ниже]")[0].Click();

        Assert.Equal(["Аня", "Вася", "Боря"], Names(page));
        Assert.Equal(["1", "2"], SeatNumbers(page));
    }

    private static List<string> Names(IRenderedComponent<NewMatch> page) =>
        page.FindAll(".mud-checkbox .mud-typography, .mud-checkbox-label, label")
            .Select(element => element.TextContent.Trim())
            .Where(text => text is "Аня" or "Боря" or "Вася")
            .Distinct()
            .ToList();

    private static List<string> SeatNumbers(IRenderedComponent<NewMatch> page) =>
        page.FindAll(".mud-typography-caption")
            .Select(element => element.TextContent.Trim())
            .Where(text => text is "1" or "2" or "3")
            .ToList();

    private sealed class StubPlayerRepository : IPlayerRepository
    {
        private readonly List<Player> _players = new();
        private int _nextId;

        public Player Seed(string name)
        {
            var player = Player.Create(name, new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero)).Value!;
            typeof(Player)
                .GetProperty(nameof(Player.Id))!
                .GetSetMethod(nonPublic: true)!
                .Invoke(player, new object[] { ++_nextId });
            _players.Add(player);
            return player;
        }

        public Task<IReadOnlyList<Player>> ListAsync(bool includeArchived, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Player>>(_players.ToList());

        public Task<Player?> GetAsync(int playerId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> ExistsWithNameAsync(string name, int? excludePlayerId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddAsync(Player player, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubMatchQueries : IMatchQueries
    {
        public IReadOnlyList<int> LastPlayerIds { get; set; } = [];

        public Task<IReadOnlyList<MatchListItemDto>> ListAsync(MatchListFilter filter, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<int>> GetLastParticipantPlayerIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(LastPlayerIds);
    }

    private sealed class UnusedMatchRepository : IMatchRepository
    {
        public Task<Match?> GetAsync(int matchId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> AllPlayersExistAsync(IReadOnlyList<int> playerIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
    }
}
