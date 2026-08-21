namespace DartsLeaderboard.Web.Components.Match;

public static class ThrowHeatColor
{
    private const int GreenR = 46;
    private const int GreenG = 125;
    private const int GreenB = 50;
    private const int RedR = 198;
    private const int RedG = 40;
    private const int RedB = 40;

    public static string For(int points, int min, int max)
    {
        var t = max == min ? 0.5 : (points - min) / (double)(max - min);
        t = Math.Clamp(t, 0, 1);
        // High scores → green (t=1), low → red (t=0)
        var r = (int)Math.Round(RedR + (GreenR - RedR) * t);
        var g = (int)Math.Round(RedG + (GreenG - RedG) * t);
        var b = (int)Math.Round(RedB + (GreenB - RedB) * t);
        return $"rgb({r}, {g}, {b})";
    }
}
