using DartsLeaderboard.Web.Components.Match;

namespace DartsLeaderboard.Web.Tests;

public class ThrowHeatColorTests
{
    [Fact]
    public void Midpoint_IsBetweenGreenAndRed()
    {
        var color = ThrowHeatColor.For(90, min: 0, max: 180);

        Assert.StartsWith("rgb(", color);
        Assert.DoesNotContain("46, 125, 50", color); // pure green
        Assert.DoesNotContain("198, 40, 40", color); // pure red
    }

    [Fact]
    public void Max_IsGreen()
    {
        Assert.Equal("rgb(46, 125, 50)", ThrowHeatColor.For(180, min: 0, max: 180));
    }

    [Fact]
    public void Min_IsRed()
    {
        Assert.Equal("rgb(198, 40, 40)", ThrowHeatColor.For(0, min: 0, max: 180));
    }

    [Fact]
    public void EqualRange_UsesMidTone()
    {
        Assert.Equal("rgb(122, 82, 45)", ThrowHeatColor.For(60, min: 60, max: 60));
    }
}
