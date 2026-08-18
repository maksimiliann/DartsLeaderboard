using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Players;
using DartsLeaderboard.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Tests;

[Collection(nameof(PostgresCollection))]
public class PersistenceTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Migrations_CreateSchema()
    {
        await using var context = fixture.CreateContext();

        Assert.Empty(await context.Players.Where(p => p.Name == "нет такого").ToListAsync());
    }

    [Fact]
    public async Task PlayerRepository_AddsAndFindsByNameIgnoringCase()
    {
        await using var context = fixture.CreateContext();
        var repository = new PlayerRepository(context);

        await repository.AddAsync(Player.Create("Максим-" + Guid.NewGuid().ToString("N")[..6], Now).Value!, default);
        await repository.SaveChangesAsync(default);

        var stored = (await repository.ListAsync(includeArchived: true, default)).Last();
        Assert.True(await repository.ExistsWithNameAsync(stored.Name.ToUpperInvariant(), null, default));
        Assert.False(await repository.ExistsWithNameAsync(stored.Name, stored.Id, default));
    }

    [Fact]
    public async Task MatchRepository_RoundTripsAggregateWithThrows()
    {
        int matchId;

        await using (var context = fixture.CreateContext())
        {
            var players = new PlayerRepository(context);
            var first = Player.Create("Игрок-" + Guid.NewGuid().ToString("N")[..6], Now).Value!;
            var second = Player.Create("Игрок-" + Guid.NewGuid().ToString("N")[..6], Now).Value!;
            await players.AddAsync(first, default);
            await players.AddAsync(second, default);
            await players.SaveChangesAsync(default);

            var matches = new MatchRepository(context);
            var match = Match.Start(MatchSettings.X01(301), new[] { first.Id, second.Id }, Now).Value!;
            await matches.AddAsync(match, default);
            await matches.SaveChangesAsync(default);

            match.RecordThrow(60, Now);
            match.RecordThrow(45, Now);
            await matches.SaveChangesAsync(default);

            matchId = match.Id;
        }

        await using var verifyContext = fixture.CreateContext();
        var restored = await new MatchRepository(verifyContext).GetAsync(matchId, default);

        Assert.NotNull(restored);
        Assert.Equal(2, restored!.Participants.Count);
        Assert.Equal(2, restored.Throws.Count);
        Assert.Equal(60, restored.Throws[0].Points);
        Assert.Equal(2, restored.CurrentRoundNumber);
        Assert.NotNull(restored.Participants[0].Player);
        Assert.Equal(241, restored.Rules.RunningValueAfterRound(restored, restored.Participants[0], 1));
    }

    [Fact]
    public async Task MatchRepository_TranslatesDuplicateRoundIntoConflict()
    {
        await using var context = fixture.CreateContext();
        var players = new PlayerRepository(context);
        var first = Player.Create("Игрок-" + Guid.NewGuid().ToString("N")[..6], Now).Value!;
        var second = Player.Create("Игрок-" + Guid.NewGuid().ToString("N")[..6], Now).Value!;
        await players.AddAsync(first, default);
        await players.AddAsync(second, default);
        await players.SaveChangesAsync(default);

        var matches = new MatchRepository(context);
        var match = Match.Start(MatchSettings.X01(301), new[] { first.Id, second.Id }, Now).Value!;
        await matches.AddAsync(match, default);
        await matches.SaveChangesAsync(default);

        match.RecordThrow(60, Now);
        await matches.SaveChangesAsync(default);

        // Второе устройство прочитало матч до записи и пишет тот же раунд заново.
        await using var secondContext = fixture.CreateContext();
        var secondMatches = new MatchRepository(secondContext);
        var sameMatch = (await secondMatches.GetAsync(match.Id, default))!;
        sameMatch.UndoLastThrow();
        sameMatch.RecordThrow(20, Now);
        secondContext.Entry(sameMatch.Throws[0]).State = EntityState.Added;

        await Assert.ThrowsAsync<MatchConflictException>(() => secondMatches.SaveChangesAsync(default));
    }
}
