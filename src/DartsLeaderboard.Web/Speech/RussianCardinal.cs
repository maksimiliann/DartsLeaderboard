namespace DartsLeaderboard.Web.Speech;

public static class RussianCardinal
{
    private static readonly string[] OnesMasculine = ["", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять"];
    private static readonly string[] OnesFeminine = ["", "одна", "две", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять"];
    private static readonly string[] OnesNeuter = ["", "одно", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять"];
    private static readonly string[] Teens =
    [
        "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать",
        "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
    ];
    private static readonly string[] Tens =
    [
        "", "", "двадцать", "тридцать", "сорок", "пятьдесят",
        "шестьдесят", "семьдесят", "восемьдесят", "девяносто"
    ];
    private static readonly string[] Hundreds =
    [
        "", "сто", "двести", "триста", "четыреста",
        "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот"
    ];

    public static string Points(int total)
    {
        var value = Math.Abs(total);
        return $"{Spell(value, Gender.Neuter)} {PointsWord(value)}";
    }

    private static string Spell(int value, Gender gender)
    {
        if (value == 0)
        {
            return "ноль";
        }

        var parts = new List<string>();
        if (value >= 1000)
        {
            var thousands = value / 1000;
            parts.Add(Spell(thousands, Gender.Feminine));
            parts.Add(ThousandWord(thousands));
            value %= 1000;
        }

        if (value >= 100)
        {
            parts.Add(Hundreds[value / 100]);
            value %= 100;
        }

        if (value >= 20)
        {
            parts.Add(Tens[value / 10]);
            value %= 10;
        }
        else if (value >= 10)
        {
            parts.Add(Teens[value - 10]);
            value = 0;
        }

        if (value > 0)
        {
            parts.Add(gender switch
            {
                Gender.Feminine => OnesFeminine[value],
                Gender.Neuter => OnesNeuter[value],
                _ => OnesMasculine[value]
            });
        }

        return string.Join(' ', parts);
    }

    private static string ThousandWord(int value)
    {
        var tail = value % 100;
        if (tail is >= 11 and <= 14)
        {
            return "тысяч";
        }

        return (value % 10) switch
        {
            1 => "тысяча",
            2 or 3 or 4 => "тысячи",
            _ => "тысяч"
        };
    }

    private static string PointsWord(int value)
    {
        var tail = value % 100;
        if (tail is >= 11 and <= 14)
        {
            return "очков";
        }

        return (value % 10) switch
        {
            1 => "очко",
            2 or 3 or 4 => "очка",
            _ => "очков"
        };
    }

    private enum Gender
    {
        Masculine,
        Feminine,
        Neuter
    }
}
