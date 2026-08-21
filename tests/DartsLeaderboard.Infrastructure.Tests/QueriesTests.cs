using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Reports;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Players;
using DartsLeaderboard.Infrastructure.Persistence.Entities;
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
        var match = Match.Start(MatchSettings.X01(40), new[] { winner.Id, loser.Id }, Now).Value!;
        await matches.AddAsync(match, default);
        await matches.SaveChangesAsync(default);
        match.RecordThrow([new VisitDart(40, true)], Now);
        await matches.SaveChangesAsync(default);

        var rows = await new LeaderboardQueries(context).GetAsync(default);

        var winnerRow = rows.Single(r => r.PlayerId == winner.Id);
        var loserRow = rows.Single(r => r.PlayerId == loser.Id);
        Assert.Equal(1, winnerRow.WinsX01);
        Assert.Equal(0, winnerRow.WinsHighestTotal);
        Assert.Equal(1, winnerRow.MatchesPlayed);
        Assert.Equal(0, loserRow.WinsX01);
        Assert.Equal(0, loserRow.WinsHighestTotal);
        Assert.Equal(1, loserRow.MatchesPlayed);
    }

    [Fact]
    public async Task Leaderboard_SplitsWinsByMode()
    {
        await using var context = fixture.CreateContext();
        var players = new PlayerRepository(context);
        var winner = Player.Create(UniqueName(), Now).Value!;
        var loser = Player.Create(UniqueName(), Now).Value!;
        await players.AddAsync(winner, default);
        await players.AddAsync(loser, default);
        await players.SaveChangesAsync(default);

        var matches = new MatchRepository(context);
        var x01 = Match.Start(MatchSettings.X01(40), new[] { winner.Id, loser.Id }, Now).Value!;
        var highest = Match.Start(MatchSettings.HighestTotal(1), new[] { winner.Id, loser.Id }, Now).Value!;
        await matches.AddAsync(x01, default);
        await matches.AddAsync(highest, default);
        await matches.SaveChangesAsync(default);
        x01.RecordThrow([new VisitDart(40, true)], Now);
        highest.RecordThrow(60, Now);
        highest.RecordThrow(10, Now);
        await matches.SaveChangesAsync(default);

        var rows = await new LeaderboardQueries(context).GetAsync(default);

        var winnerRow = rows.Single(r => r.PlayerId == winner.Id);
        var loserRow = rows.Single(r => r.PlayerId == loser.Id);
        Assert.Equal(1, winnerRow.WinsX01);
        Assert.Equal(1, winnerRow.WinsHighestTotal);
        Assert.Equal(2, winnerRow.MatchesPlayed);
        Assert.Equal(0, loserRow.WinsX01);
        Assert.Equal(0, loserRow.WinsHighestTotal);
        Assert.Equal(2, loserRow.MatchesPlayed);
    }

    [Fact]
    public async Task Leaderboard_AddsArchiveWinsToLiveTotals()
    {
        await using var context = fixture.CreateContext();
        var players = new PlayerRepository(context);
        var player = Player.Create(UniqueName(), Now).Value!;
        var other = Player.Create(UniqueName(), Now).Value!;
        await players.AddAsync(player, default);
        await players.AddAsync(other, default);
        await players.SaveChangesAsync(default);

        var matches = new MatchRepository(context);
        var match = Match.Start(MatchSettings.X01(40), new[] { player.Id, other.Id }, Now).Value!;
        await matches.AddAsync(match, default);
        await matches.SaveChangesAsync(default);
        match.RecordThrow([new VisitDart(40, true)], Now);
        await matches.SaveChangesAsync(default);

        context.WinArchives.Add(new PlayerWinArchive
        {
            PlayerId = player.Id,
            WinsX01 = 3,
            WinsHighestTotal = 2
        });
        await context.SaveChangesAsync(default);

        var row = (await new LeaderboardQueries(context).GetAsync(default))
            .Single(r => r.PlayerId == player.Id);

        Assert.Equal(4, row.WinsX01);
        Assert.Equal(2, row.WinsHighestTotal);
        Assert.Equal(1, row.MatchesPlayed);
        Assert.Equal(1, row.LiveWins);
        Assert.Equal(1d, row.WinRate);
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
        var finished = Match.Start(MatchSettings.X01(40), new[] { first.Id, second.Id }, Now).Value!;
        var running = Match.Start(MatchSettings.HighestTotal(5), new[] { first.Id, second.Id }, Now).Value!;
        await matches.AddAsync(finished, default);
        await matches.AddAsync(running, default);
        await matches.SaveChangesAsync(default);
        finished.RecordThrow([new VisitDart(40, true)], Now);
        await matches.SaveChangesAsync(default);

        var queries = new MatchQueries(context);
        var finishedList = await queries.ListAsync(MatchListFilter.Finished, default);
        var runningList = await queries.ListAsync(MatchListFilter.InProgress, default);

        var finishedItem = finishedList.Single(m => m.MatchId == finished.Id);
        Assert.Equal("40 на очки", finishedItem.ModeTitle);
        Assert.Equal(first.Name, finishedItem.WinnerName);
        Assert.Contains(first.Name, finishedItem.Participants);

        var runningItem = runningList.Single(m => m.MatchId == running.Id);
        Assert.Equal("Максимум за 5 раундов", runningItem.ModeTitle);
        Assert.Null(runningItem.WinnerName);
    }

    [Fact]
    public async Task Records_TrackBestThrowAndFiveRoundMatch()
    {
        await using var context = fixture.CreateContext();
        var players = new PlayerRepository(context);
        var throwHolder = Player.Create(UniqueName(), Now).Value!;
        var fiveHolder = Player.Create(UniqueName(), Now).Value!;
        var other = Player.Create(UniqueName(), Now).Value!;
        await players.AddAsync(throwHolder, default);
        await players.AddAsync(fiveHolder, default);
        await players.AddAsync(other, default);
        await players.SaveChangesAsync(default);

        var matches = new MatchRepository(context);
        var throwMatch = Match.Start(MatchSettings.HighestTotal(1), new[] { throwHolder.Id, other.Id }, Now).Value!;
        var fiveMatch = Match.Start(MatchSettings.HighestTotal(5), new[] { fiveHolder.Id, other.Id }, Now).Value!;
        await matches.AddAsync(throwMatch, default);
        await matches.AddAsync(fiveMatch, default);
        await matches.SaveChangesAsync(default);

        throwMatch.RecordThrow(180, Now);
        throwMatch.RecordThrow(0, Now.AddMinutes(1));

        for (var round = 0; round < 5; round++)
        {
            fiveMatch.RecordThrow(30, Now.AddMinutes(10 + round * 2));
            fiveMatch.RecordThrow(5, Now.AddMinutes(11 + round * 2));
        }

        await matches.SaveChangesAsync(default);

        var records = await new RecordQueries(context).GetAsync(default);
        var bestThrow = records.Single(r => r.Key == ClubRecords.BestThrowKey);
        var bestFive = records.Single(r => r.Key == ClubRecords.BestFiveKey);

        Assert.Equal(180, bestThrow.Value);
        Assert.Equal(throwHolder.Name, bestThrow.HolderName);
        Assert.Equal(150, bestFive.Value);
        Assert.Equal(fiveHolder.Name, bestFive.HolderName);
    }

    [Fact]
    public async Task LastParticipants_ReturnPlayerIdsFromLatestMatchInSeatOrder()
    {
        await using var context = fixture.CreateContext();
        var players = new PlayerRepository(context);
        var first = Player.Create(UniqueName(), Now).Value!;
        var second = Player.Create(UniqueName(), Now).Value!;
        var third = Player.Create(UniqueName(), Now).Value!;
        await players.AddAsync(first, default);
        await players.AddAsync(second, default);
        await players.AddAsync(third, default);
        await players.SaveChangesAsync(default);

        var matches = new MatchRepository(context);
        var older = Match.Start(MatchSettings.X01(101), new[] { first.Id, second.Id }, Now).Value!;
        var latest = Match.Start(
            MatchSettings.HighestTotal(5),
            new[] { third.Id, first.Id },
            new DateTimeOffset(2099, 1, 1, 12, 0, 0, TimeSpan.Zero)).Value!;
        await matches.AddAsync(older, default);
        await matches.AddAsync(latest, default);
        await matches.SaveChangesAsync(default);

        var ids = await new MatchQueries(context).GetLastParticipantPlayerIdsAsync(default);

        Assert.Equal(new[] { third.Id, first.Id }, ids);
    }
}
