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

    public static string ButtonText(SpeechPlayback playback) => playback switch
    {
        SpeechPlayback.Playing => "Пауза",
        SpeechPlayback.Paused => "Продолжить",
        _ => "Озвучить результат"
    };

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

        var text = new StringBuilder("<speak><prosody rate=\"slow\">");
        foreach (var row in standings)
        {
            text.Append("<s>");
            text.Append(Place(row.Place));
            text.Append(" место. ");
            text.Append(WebUtility.HtmlEncode(row.PlayerName));
            text.Append(". ");
            text.Append(RussianCardinal.Points(row.Total));
            text.Append(".</s>");
        }

        text.Append("</prosody></speak>");
        return text.ToString();
    }

    private static string Place(int place) =>
        place is >= 1 and <= 10 ? Places[place - 1] : place.ToString();
}

public enum SpeechPlayback
{
    Idle,
    Playing,
    Paused
}
