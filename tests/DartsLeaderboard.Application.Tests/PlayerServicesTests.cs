using DartsLeaderboard.Application.Players;
using DartsLeaderboard.Application.Tests.Fakes;

namespace DartsLeaderboard.Application.Tests;

public class PlayerServicesTests
{
    private readonly FakePlayerRepository _repository = new();
    private readonly FixedClock _clock = new();

    [Fact]
    public async Task AddPlayer_SavesTrimmedName()
    {
        var service = new AddPlayerService(_repository, _clock);

        var result = await service.ExecuteAsync("  Максим ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Максим", result.Value!.Name);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task AddPlayer_RejectsDuplicateNameIgnoringCase()
    {
        _repository.Seed("Максим");
        var service = new AddPlayerService(_repository, _clock);

        var result = await service.ExecuteAsync("максим");

        Assert.False(result.IsSuccess);
        Assert.Equal("Игрок с таким именем уже есть", result.ErrorMessage);
    }

    [Fact]
    public async Task AddPlayer_RejectsEmptyName()
    {
        var service = new AddPlayerService(_repository, _clock);

        var result = await service.ExecuteAsync("   ");

        Assert.Equal("Имя игрока не может быть пустым", result.ErrorMessage);
    }

    [Fact]
    public async Task GetPlayers_HidesArchivedByDefault()
    {
        _repository.Seed("Максим");
        _repository.Seed("Пётр", archived: true);
        var service = new GetPlayersService(_repository);

        var visible = await service.ExecuteAsync(includeArchived: false);
        var all = await service.ExecuteAsync(includeArchived: true);

        Assert.Single(visible);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task RenamePlayer_UpdatesName()
    {
        var player = _repository.Seed("Аня");
        var service = new RenamePlayerService(_repository);

        var result = await service.ExecuteAsync(player.Id, "Анна");

        Assert.True(result.IsSuccess);
        Assert.Equal("Анна", player.Name);
    }

    [Fact]
    public async Task RenamePlayer_FailsForUnknownPlayer()
    {
        var service = new RenamePlayerService(_repository);

        var result = await service.ExecuteAsync(42, "Анна");

        Assert.Equal("Игрок не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task SetArchived_TogglesFlag()
    {
        var player = _repository.Seed("Пётр");
        var service = new SetPlayerArchivedService(_repository);

        await service.ExecuteAsync(player.Id, archived: true);

        Assert.True(player.IsArchived);
    }
}
