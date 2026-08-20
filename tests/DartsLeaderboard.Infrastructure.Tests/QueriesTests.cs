using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Players;
using DartsLeaderboard.Infrastructure.Persistence.Queries;
using DartsLeaderboard.Infrastructure.Persistence.Repositories;

namespace DartsLeaderboard.Infrastructure.Tests;

[Collection(nameof(PostgresCollection))]
public class QueriesTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    private static string UniqueName() => "Игрок-" + Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Leaderboard_CountsWinsAndMatches()
    {
        await using var context = fixture.CreateContext();
        var players = new PlayerRepository(context);
        var winner = Player.Create(UniqueName(), Now).Value!;
        var loser = Player.Create(UniqueName(), Now).Value!;
        await players.AddAsync(winner, default);
        await players.AddAsync(loser, default);
        await players.SaveChangesAsync(default);

        var matches = new MatchRepository(context);
        var match = Match.Start(MatchSettings.X01(101), new[] { winner.Id, loser.Id }, Now).Value!;
        await matches.AddAsync(match, default);
        await matches.SaveChangesAsync(default);
        match.RecordThrow(101, Now);
        await matches.SaveChangesAsync(default);

        var rows = await new LeaderboardQueries(context).GetAsync(default);

        var winnerRow = rows.Single(r => r.PlayerId == winner.Id);
        var loserRow = rows.Single(r => r.PlayerId == loser.Id);
        Assert.Equal(1, winnerRow.Wins);
        Assert.Equal(1, winnerRow.MatchesPlayed);
        Assert.Equal(0, loserRow.Wins);
        Assert.Equal(1, loserRow.MatchesPlayed);
    }

    [Fact]
    public async Task MatchList_SplitsInProgressAndFinished()
    {
        await using var context = fixture.CreateContext();
        var players = new PlayerRepository(context);
        var first = Player.Create(UniqueName(), Now).Value!;
        var second = Player.Create(UniqueName(), Now).Value!;
        await players.AddAsync(first, default);
        await players.AddAsync(second, default);
        await players.SaveChangesAsync(default);

        var matches = new MatchRepository(context);
        var finished = Match.Start(MatchSettings.X01(101), new[] { first.Id, second.Id }, Now).Value!;
        var running = Match.Start(MatchSettings.HighestTotal(5), new[] { first.Id, second.Id }, Now).Value!;
        await matches.AddAsync(finished, default);
        await matches.AddAsync(running, default);
        await matches.SaveChangesAsync(default);
        finished.RecordThrow(101, Now);
        await matches.SaveChangesAsync(default);

        var queries = new MatchQueries(context);
        var finishedList = await queries.ListAsync(MatchListFilter.Finished, default);
        var runningList = await queries.ListAsync(MatchListFilter.InProgress, default);

        var finishedItem = finishedList.Single(m => m.MatchId == finished.Id);
        Assert.Equal("101 на очки", finishedItem.ModeTitle);
        Assert.Equal(first.Name, finishedItem.WinnerName);
        Assert.Contains(first.Name, finishedItem.Participants);

        var runningItem = runningList.Single(m => m.MatchId == running.Id);
        Assert.Equal("Максимум за 5 раундов", runningItem.ModeTitle);
        Assert.Null(runningItem.WinnerName);
    }
}
