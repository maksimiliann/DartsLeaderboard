using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Tests;

public class LeaderboardRowDtoTests
{
    [Fact]
    public void WinRate_UsesLiveWinsOnly()
    {
        var row = new LeaderboardRowDto(
            1,
            "Игрок",
            WinsX01: 11,
            WinsHighestTotal: 5,
            MatchesPlayed: 4,
            LiveWins: 2);

        Assert.Equal(16, row.Wins);
        Assert.Equal(0.5, row.WinRate);
    }
}
