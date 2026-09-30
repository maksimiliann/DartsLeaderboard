using System.Net;
using System.Text;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Web.Speech;

public static class ResultAnnouncement
{
    private static readonly string[] Places =
    [
        "Первое",
        "Второе",
        "Третье",
        "Четвёртое",
        "Пятое",
        "Шестое",
        "Седьмое",
        "Восьмое",
        "Девятое",
        "Десятое"
    ];

    public static bool CanSpeak(MatchStateDto state) =>
        !state.IsInProgress &&
        state.StatusTitle is "Завершён" or "Ничья" &&
        state.Standings.Count > 0;

    public static string Format(IReadOnlyList<StandingDto> standings)
    {
        if (standings.Count == 0)
        {
            return string.Empty;
        }

        var text = new StringBuilder("<speak>");
        for (var i = 0; i < standings.Count; i++)
        {
            if (i > 0)
            {
                text.Append("<break time=\"400ms\"/>");
            }

            var row = standings[i];
            text.Append(Place(row.Place));
            text.Append(" место. ");
            text.Append(WebUtility.HtmlEncode(row.PlayerName));
            text.Append(". ");
            text.Append(row.Total);
            text.Append(' ');
            text.Append(PointsWord(row.Total));
            text.Append('.');
        }

        text.Append("</speak>");
        return text.ToString();
    }

    private static string Place(int place) =>
        place is >= 1 and <= 10 ? Places[place - 1] : place.ToString();

    private static string PointsWord(int total)
    {
        var abs = Math.Abs(total) % 100;
        var last = abs % 10;
        if (abs is >= 11 and <= 14)
        {
            return "очков";
        }

        return last switch
        {
            1 => "очко",
            2 or 3 or 4 => "очка",
            _ => "очков"
        };
    }
}
