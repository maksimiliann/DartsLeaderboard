using DartsLeaderboard.Application.Reports;

namespace DartsLeaderboard.Application.Tests;

public class WeekRangeTests
{
    [Fact]
    public void Containing_UsesMoscowMondayToMonday()
    {
        var utc = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

        var (start, end) = WeekRange.Containing(utc);

        Assert.Equal(new DateTimeOffset(2026, 9, 6, 21, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 9, 13, 21, 0, 0, TimeSpan.Zero), end);
        Assert.Equal(TimeSpan.Zero, start.Offset);
        Assert.Equal(TimeSpan.Zero, end.Offset);
    }

    [Fact]
    public void Containing_KeepsSundayEveningMoscowInCurrentWeek()
    {
        var sundayEveningMsk = new DateTimeOffset(2026, 9, 13, 23, 0, 0, TimeSpan.FromHours(3));

        var (start, end) = WeekRange.Containing(sundayEveningMsk.ToUniversalTime());

        Assert.Equal(new DateTimeOffset(2026, 9, 6, 21, 0, 0, TimeSpan.Zero), start);
        Assert.True(sundayEveningMsk >= start && sundayEveningMsk < end);
    }

    [Fact]
    public void FormatLabel_ShowsMoscowInclusiveDays()
    {
        var (start, end) = WeekRange.Containing(new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal("7–13 сентября", WeekRange.FormatLabel(start, end));
    }
}
