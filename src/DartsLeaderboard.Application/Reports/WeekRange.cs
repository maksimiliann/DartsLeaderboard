namespace DartsLeaderboard.Application.Reports;

public static class WeekRange
{
    private static readonly string[] MonthGenitive =
    [
        "января", "февраля", "марта", "апреля", "мая", "июня",
        "июля", "августа", "сентября", "октября", "ноября", "декабря"
    ];

    public static readonly TimeZoneInfo Zone = ResolveMoscow();

    private static TimeZoneInfo ResolveMoscow()
    {
        foreach (var id in new[] { "Europe/Moscow", "Russian Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Moscow", TimeSpan.FromHours(3), "Moscow", "Moscow");
    }

    public static (DateTimeOffset Start, DateTimeOffset End) Containing(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, Zone);
        var daysFromMonday = ((int)local.DayOfWeek + 6) % 7;
        var monday = DateOnly.FromDateTime(local.DateTime).AddDays(-daysFromMonday);
        var startLocal = monday.ToDateTime(TimeOnly.MinValue);
        var start = new DateTimeOffset(startLocal, Zone.GetUtcOffset(startLocal)).ToUniversalTime();
        return (start, start.AddDays(7));
    }

    public static string FormatLabel(DateTimeOffset start, DateTimeOffset end)
    {
        var startLocal = TimeZoneInfo.ConvertTime(start, Zone);
        var lastLocal = TimeZoneInfo.ConvertTime(end, Zone).AddTicks(-1);
        if (startLocal.Month == lastLocal.Month && startLocal.Year == lastLocal.Year)
        {
            return $"{startLocal.Day}–{lastLocal.Day} {MonthName(lastLocal)}";
        }

        return $"{startLocal.Day} {MonthName(startLocal)} – {lastLocal.Day} {MonthName(lastLocal)}";
    }

    private static string MonthName(DateTimeOffset value) => MonthGenitive[value.Month - 1];
}
