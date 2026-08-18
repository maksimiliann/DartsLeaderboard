using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Players;

namespace DartsLeaderboard.Domain.Tests;

public class PlayerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_TrimsName()
    {
        var result = Player.Create("  Максим  ", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("Максим", result.Value!.Name);
        Assert.False(result.Value.IsArchived);
        Assert.Equal(Now, result.Value.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmptyName(string name)
    {
        var result = Player.Create(name, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCode.PlayerNameEmpty, result.Error);
    }

    [Fact]
    public void Create_RejectsTooLongName()
    {
        var result = Player.Create(new string('a', 51), Now);

        Assert.Equal(DomainErrorCode.PlayerNameTooLong, result.Error);
    }

    [Fact]
    public void Rename_ChangesName()
    {
        var player = Player.Create("Аня", Now).Value!;

        var result = player.Rename(" Анна ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Анна", player.Name);
    }

    [Fact]
    public void SetArchived_TogglesFlag()
    {
        var player = Player.Create("Пётр", Now).Value!;

        player.SetArchived(true);
        Assert.True(player.IsArchived);

        player.SetArchived(false);
        Assert.False(player.IsArchived);
    }
}
