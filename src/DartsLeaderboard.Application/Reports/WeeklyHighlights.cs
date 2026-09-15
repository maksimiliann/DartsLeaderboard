using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Reports;

public static class WeeklyHighlights
{
    public const string MostWinsKey = "most-wins";
    public const string MostLossesKey = "most-losses";
    public const string MostX01WinsKey = "most-x01-wins";
    public const string MostHighestTotalWinsKey = "most-highest-total-wins";
    public const string BestVisitKey = "best-visit";
    public const string WorstVisitKey = "worst-visit";
    public const string MaxSumKey = "max-sum";
    public const string MinSumKey = "min-sum";
    public const string ClosestToAverageKey = "closest-to-average";
    public const string Most26Key = "most-26";
    public const string Most21Key = "most-21";
    public const string FastestX01Key = "fastest-x01";
    public const string SlowestX01Key = "slowest-x01";
    public const string FastestMatchKey = "fastest-match";
    public const string LongestMatchKey = "longest-match";

    public const string MostWinsTitle = "Наныл недели";
    public const string MostLossesTitle = "Никогда не сдавайся";
    public const string MostX01WinsTitle = "Меткий глаз";
    public const string MostHighestTotalWinsTitle = "И ветер не помеха";
    public const string BestVisitTitle = "Повезло, повезло";
    public const string WorstVisitTitle = "Опять дротики виноваты";
    public const string MaxSumTitle = "Мистер зеленый";
    public const string MinSumTitle = "Мистер красный";
    public const string ClosestToAverageTitle = "Мистер коричневый";
    public const string Most26Title = "За ваше здоровье...";
    public const string Most21Title = "...и за мое очко";
    public const string FastestX01Title = "Скорострел";
    public const string SlowestX01Title = "Тугосеря";
    public const string FastestMatchTitle = "Может еще одну?";
    public const string LongestMatchTitle = "Лишь бы не работать";

    public const string MostWinsDescription = "Больше всех побед за неделю во всех режимах";
    public const string MostLossesDescription = "Не победы в x01 и последние места в «максимуме»";
    public const string MostX01WinsDescription = "Больше всех побед в x01";
    public const string MostHighestTotalWinsDescription = "Больше всех побед в «максимуме»";
    public const string BestVisitDescription = "Лучший бросок в «максимуме»";
    public const string WorstVisitDescription = "Худший бросок в «максимуме»";
    public const string MaxSumDescription = "Максимальная сумма очков за неделю в «максимуме»";
    public const string MinSumDescription = "Минимальная сумма очков за неделю в «максимуме»";
    public const string ClosestToAverageDescription = "Сумма в «максимуме» ближе всех к средней по игрокам";
    public const string Most26Description = "Больше всех бросков на 26";
    public const string Most21Description = "Больше всех бросков на 21";
    public const string FastestX01Description = "Закрыл x01 за наименьшее число раундов";
    public const string SlowestX01Description = "Закрыл x01 за наибольшее число раундов";
    public const string FastestMatchDescription = "Самый быстрый матч по времени";
    public const string LongestMatchDescription = "Самый долгий матч по времени";

    public static IReadOnlyList<WeeklyHighlightDto> From(
        IReadOnlyList<WeeklyPlayerFacts> players,
        IReadOnlyList<WeeklyMatchFacts> matches) =>
        [
            PickMax(MostWinsKey, MostWinsTitle, players, p => Positive(p.Wins)),
            PickMax(MostLossesKey, MostLossesTitle, players, p => Positive(p.Losses)),
            PickMax(MostX01WinsKey, MostX01WinsTitle, players, p => Positive(p.WinsX01)),
            PickMax(MostHighestTotalWinsKey, MostHighestTotalWinsTitle, players, p => Positive(p.WinsHighestTotal)),
            PickMax(BestVisitKey, BestVisitTitle, players, p => p.BestVisit),
            PickMin(WorstVisitKey, WorstVisitTitle, players, p => p.WorstVisit),
            PickMax(MaxSumKey, MaxSumTitle, players, p => p.HighestTotalSum),
            PickMin(MinSumKey, MinSumTitle, players, p => p.HighestTotalSum),
            PickClosestToAverage(players),
            PickMax(Most26Key, Most26Title, players, p => Positive(p.Count26)),
            PickMax(Most21Key, Most21Title, players, p => Positive(p.Count21)),
            PickMin(FastestX01Key, FastestX01Title, players, p => p.FastestX01Rounds),
            PickMax(SlowestX01Key, SlowestX01Title, players, p => p.SlowestX01Rounds),
            PickMatch(FastestMatchKey, FastestMatchTitle, matches, longest: false),
            PickMatch(LongestMatchKey, LongestMatchTitle, matches, longest: true)
        ];

    private static int? Positive(int value) => value > 0 ? value : null;

    private static WeeklyHighlightDto PickMax(
        string key,
        string title,
        IEnumerable<WeeklyPlayerFacts> players,
        Func<WeeklyPlayerFacts, int?> value) =>
        Pick(key, title, players, value, descending: true);

    private static WeeklyHighlightDto PickMin(
        string key,
        string title,
        IEnumerable<WeeklyPlayerFacts> players,
        Func<WeeklyPlayerFacts, int?> value) =>
        Pick(key, title, players, value, descending: false);

    private static WeeklyHighlightDto Pick(
        string key,
        string title,
        IEnumerable<WeeklyPlayerFacts> players,
        Func<WeeklyPlayerFacts, int?> value,
        bool descending)
    {
        var candidates = players
            .Select(player => new { player.Name, Value = value(player) })
            .Where(row => row.Value is not null)
            .ToList();
        if (candidates.Count == 0)
        {
            return Unset(key, title);
        }

        var bestValue = descending
            ? candidates.Max(row => row.Value)
            : candidates.Min(row => row.Value);
        var holders = candidates
            .Where(row => row.Value == bestValue)
            .OrderBy(row => row.Name, StringComparer.Ordinal)
            .ToList();

        return Highlight(
            key,
            title,
            JoinNames(holders.Select(row => row.Name)),
            bestValue!.Value.ToString());
    }

    private static WeeklyHighlightDto PickClosestToAverage(IReadOnlyList<WeeklyPlayerFacts> players)
    {
        var withSum = players.Where(player => player.HighestTotalSum is not null).ToList();
        if (withSum.Count == 0)
        {
            return Unset(ClosestToAverageKey, ClosestToAverageTitle);
        }

        var average = withSum.Average(player => player.HighestTotalSum!.Value);
        var bestDistance = withSum.Min(player => Math.Abs(player.HighestTotalSum!.Value - average));
        var holders = withSum
            .Where(player => Math.Abs(player.HighestTotalSum!.Value - average) == bestDistance)
            .OrderBy(player => player.Name, StringComparer.Ordinal)
            .ToList();

        return Highlight(
            ClosestToAverageKey,
            ClosestToAverageTitle,
            JoinNames(holders.Select(player => player.Name)),
            JoinNames(holders.Select(player => player.HighestTotalSum!.Value.ToString()).Distinct()));
    }

    private static WeeklyHighlightDto PickMatch(
        string key,
        string title,
        IReadOnlyList<WeeklyMatchFacts> matches,
        bool longest)
    {
        if (matches.Count == 0)
        {
            return Unset(key, title);
        }

        var bestDuration = longest
            ? matches.Max(match => match.Duration)
            : matches.Min(match => match.Duration);
        var names = matches
            .Where(match => match.Duration == bestDuration)
            .SelectMany(match => match.Participants.Split(", ", StringSplitOptions.None))
            .Distinct(StringComparer.Ordinal);

        return Highlight(key, title, JoinNames(names), FormatDuration(bestDuration));
    }

    private static string JoinNames(IEnumerable<string> names) => string.Join(", ", names);

    internal static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        return $"{(int)duration.TotalMinutes}:{duration.Seconds:D2}";
    }

    private static WeeklyHighlightDto Unset(string key, string title) =>
        Highlight(key, title, null, null);

    private static WeeklyHighlightDto Highlight(string key, string title, string? holderName, string? value) =>
        new(key, title, DescriptionFor(key), holderName, value);

    private static string DescriptionFor(string key) => key switch
    {
        MostWinsKey => MostWinsDescription,
        MostLossesKey => MostLossesDescription,
        MostX01WinsKey => MostX01WinsDescription,
        MostHighestTotalWinsKey => MostHighestTotalWinsDescription,
        BestVisitKey => BestVisitDescription,
        WorstVisitKey => WorstVisitDescription,
        MaxSumKey => MaxSumDescription,
        MinSumKey => MinSumDescription,
        ClosestToAverageKey => ClosestToAverageDescription,
        Most26Key => Most26Description,
        Most21Key => Most21Description,
        FastestX01Key => FastestX01Description,
        SlowestX01Key => SlowestX01Description,
        FastestMatchKey => FastestMatchDescription,
        LongestMatchKey => LongestMatchDescription,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
    };
}
