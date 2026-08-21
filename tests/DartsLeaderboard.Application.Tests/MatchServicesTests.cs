using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Matches;
using DartsLeaderboard.Application.Tests.Fakes;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Tests;

public class MatchServicesTests
{
    private readonly FakeMatchRepository _matches = new();
    private readonly FakePlayerRepository _players = new();
    private readonly RecordingNotifier _notifier = new();
    private readonly FixedClock _clock = new();

    [Fact]
    public async Task StartMatch_X01_CreatesMatch()
    {
        var first = _players.Seed("Максим");
        var second = _players.Seed("Аня");
        var service = new StartMatchService(_matches, _clock);

        var result = await service.ExecuteAsync(new MatchSetupRequest(
            GameModeOption.X01, 501, null, new[] { first.Id, second.Id }));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _matches.SaveCount);
    }

    [Fact]
    public async Task StartMatch_RejectsSingleParticipant()
    {
        var service = new StartMatchService(_matches, _clock);

        var result = await service.ExecuteAsync(new MatchSetupRequest(
            GameModeOption.HighestTotal, null, 5, new[] { 1 }));

        Assert.Equal("Нужно выбрать хотя бы двух игроков", result.ErrorMessage);
    }

    [Fact]
    public async Task StartMatch_RejectsUnknownPlayer()
    {
        _matches.MissingPlayerIds = new[] { 99 };
        var service = new StartMatchService(_matches, _clock);

        var result = await service.ExecuteAsync(new MatchSetupRequest(
            GameModeOption.X01, 501, null, new[] { 1, 99 }));

        Assert.Equal("Игрок не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task RecordThrow_ReturnsUpdatedStateAndNotifies()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(301), "Максим", "Аня");
        _matches.Seed(match);
        var service = new RecordThrowService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(match.Id, 60);

        Assert.True(result.IsSuccess);
        Assert.Equal(60, result.Value!.Rows[0].Cells[0].Points);
        Assert.Equal(new[] { match.Id }, _notifier.Notifications);
        Assert.Equal(1, _matches.SaveCount);
    }

    [Fact]
    public async Task RecordThrow_ReturnsErrorForUnknownMatch()
    {
        var service = new RecordThrowService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(777, 60);

        Assert.Equal("Матч не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task RecordThrow_TranslatesConflictIntoUserMessage()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(301), "Максим", "Аня");
        _matches.Seed(match);
        _matches.ThrowConflictOnSave = true;
        var service = new RecordThrowService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(match.Id, 60);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Раунд уже записан с другого устройства, состояние обновлено",
            result.ErrorMessage);
    }

    [Fact]
    public async Task RecordThrow_BustRecordsZeroAndContinues()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(101), "Максим", "Аня");
        _matches.Seed(match);
        var service = new RecordThrowService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(match.Id, 180);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Rows[0].Cells[0].Points);
        Assert.Equal("Аня", result.Value.CurrentPlayerName);
    }

    [Fact]
    public async Task RecordThrow_DoubleOutOnFirstDartFinishesMatch()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(40), "Максим", "Аня");
        _matches.Seed(match);
        var service = new RecordThrowService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(match.Id, [new ThrowDartDto(40, true)]);

        Assert.True(result.IsSuccess);
        Assert.Equal("Завершён", result.Value!.StatusTitle);
        Assert.Equal("Максим", result.Value.WinnerPlayerName);
    }

    [Fact]
    public async Task UndoLastThrow_RemovesThrow()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(301), "Максим", "Аня").WithThrows(60);
        _matches.Seed(match);
        var service = new UndoLastThrowService(_matches, _notifier);

        var result = await service.ExecuteAsync(match.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(match.Throws);
        Assert.Equal(new[] { match.Id }, _notifier.Notifications);
    }

    [Fact]
    public async Task AbandonMatch_MarksMatchAbandoned()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(301), "Максим", "Аня");
        _matches.Seed(match);
        var service = new AbandonMatchService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(match.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Abandoned, match.Status);
    }

    [Fact]
    public async Task GetMatchState_ReturnsStateForKnownMatch()
    {
        var match = TestMatchBuilder.Create(MatchSettings.HighestTotal(5), "Максим", "Аня");
        _matches.Seed(match);
        var service = new GetMatchStateService(_matches);

        var result = await service.ExecuteAsync(match.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Максимум за 5 раундов", result.Value!.ModeTitle);
    }
}
