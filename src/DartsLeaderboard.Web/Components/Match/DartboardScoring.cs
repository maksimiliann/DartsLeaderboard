using System.Globalization;

namespace DartsLeaderboard.Web.Components.Match;

public enum DartboardRing
{
    Single = 1,
    Double = 2,
    Triple = 3
}

public readonly record struct DartboardDart(string Label, int Points, bool IsDouble);

public static class DartboardScoring
{
    public static readonly int[] SectorOrder =
        [20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5];

    public static DartboardDart InnerBull { get; } = new("50", 50, true);

    public static DartboardDart OuterBull { get; } = new("25", 25, false);

    public static DartboardDart Miss { get; } = new("мимо", 0, false);

    public static DartboardDart Sector(DartboardRing ring, int sector)
    {
        if (sector is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(sector));
        }

        var multiplier = (int)ring;
        return new DartboardDart($"x{multiplier} {sector}", sector * multiplier, ring == DartboardRing.Double);
    }

    public static X01VisitResult EvaluateX01Visit(int remaining, IReadOnlyList<DartboardDart> darts)
    {
        var left = remaining;
        foreach (var dart in darts)
        {
            if (dart.Points > left)
            {
                return X01VisitResult.Bust;
            }

            left -= dart.Points;
            if (left == 1)
            {
                return X01VisitResult.Bust;
            }

            if (left == 0)
            {
                return dart.IsDouble ? X01VisitResult.Checkout : X01VisitResult.Bust;
            }
        }

        return X01VisitResult.Continue;
    }
}

public enum X01VisitResult
{
    Continue,
    Bust,
    Checkout
}

public sealed class DartboardVisit
{
    private readonly List<DartboardDart> _darts = [];

    public IReadOnlyList<DartboardDart> Darts => _darts;

    public int Sum => _darts.Sum(dart => dart.Points);

    public bool IsFull => _darts.Count >= 3;

    public bool TryAdd(DartboardDart dart)
    {
        if (IsFull)
        {
            return false;
        }

        _darts.Add(dart);
        return true;
    }

    public bool TryUndo()
    {
        if (_darts.Count == 0)
        {
            return false;
        }

        _darts.RemoveAt(_darts.Count - 1);
        return true;
    }

    public int TakeSum()
    {
        var sum = Sum;
        _darts.Clear();
        return sum;
    }

    public IReadOnlyList<DartboardDart> TakeDarts()
    {
        var copy = _darts.ToArray();
        _darts.Clear();
        return copy;
    }

    public int Bust()
    {
        _darts.Clear();
        return 0;
    }
}

public static class DartboardLayout
{
    public const double Cx = 320;
    public const double Cy = 320;
    public const double ViewBox = 640;

    public const double R50 = 28;
    public const double R25 = 58;
    /// <summary>End of inner single / start of triple.</summary>
    public const double RInnerSingle = 145;
    /// <summary>End of triple / start of outer single.</summary>
    public const double RTriple = 185;
    /// <summary>End of outer single / start of double.</summary>
    public const double ROuterSingle = 235;
    /// <summary>Outer edge of double / inner edge of number ring.</summary>
    public const double RDouble = 275;
    /// <summary>Outer edge of number / miss ring.</summary>
    public const double RMiss = 320;
    public const double RLabel = 297.5;

    public static (double X, double Y) Polar(double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return (Cx + radius * Math.Cos(radians), Cy + radius * Math.Sin(radians));
    }

    public static string RingSlice(double innerRadius, double outerRadius, double startDegrees, double endDegrees)
    {
        var (x0, y0) = Polar(outerRadius, startDegrees);
        var (x1, y1) = Polar(outerRadius, endDegrees);
        var (x2, y2) = Polar(innerRadius, endDegrees);
        var (x3, y3) = Polar(innerRadius, startDegrees);
        return string.Create(CultureInfo.InvariantCulture,
            $"M {x0:0.##} {y0:0.##} A {outerRadius:0.##} {outerRadius:0.##} 0 0 1 {x1:0.##} {y1:0.##} L {x2:0.##} {y2:0.##} A {innerRadius:0.##} {innerRadius:0.##} 0 0 0 {x3:0.##} {y3:0.##} Z");
    }

    public static (double X, double Y) HitMarker(string zoneId, int sameIndex, int sameCount)
    {
        var spread = sameCount <= 1 ? 0 : (sameIndex - (sameCount - 1) / 2.0) * 7;

        if (zoneId == "50")
        {
            return Polar(sameCount <= 1 ? 0 : 10, -90 + sameIndex * 120);
        }

        if (zoneId == "25")
        {
            return Polar((R50 + R25) / 2, -90 + spread);
        }

        var separator = zoneId.LastIndexOf('-');
        if (separator < 0)
        {
            return Polar(0, 0);
        }

        var ring = zoneId[..separator];
        var sector = int.Parse(zoneId[(separator + 1)..], CultureInfo.InvariantCulture);
        var sectorIndex = Array.IndexOf(DartboardScoring.SectorOrder, sector);
        var angle = -90 + sectorIndex * 18 + spread;
        var (inner, outer) = ring switch
        {
            "miss" => (RDouble, RMiss),
            "x1i" => (R25, RInnerSingle),
            "x3" => (RInnerSingle, RTriple),
            "x1o" => (RTriple, ROuterSingle),
            _ => (ROuterSingle, RDouble)
        };

        return Polar((inner + outer) / 2, angle);
    }
}
