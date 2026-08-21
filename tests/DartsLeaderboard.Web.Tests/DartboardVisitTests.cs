using DartsLeaderboard.Web.Components.Match;

namespace DartsLeaderboard.Web.Tests;

public class DartboardVisitTests
{
    [Theory]
    [InlineData(DartboardRing.Single, 20, "x1 20", 20)]
    [InlineData(DartboardRing.Triple, 20, "x3 20", 60)]
    [InlineData(DartboardRing.Double, 20, "x2 20", 40)]
    [InlineData(DartboardRing.Single, 1, "x1 1", 1)]
    [InlineData(DartboardRing.Double, 16, "x2 16", 32)]
    public void Sector_MapsRingAndNumberToPoints(DartboardRing ring, int sector, string label, int points)
    {
        var dart = DartboardScoring.Sector(ring, sector);

        Assert.Equal(label, dart.Label);
        Assert.Equal(points, dart.Points);
        Assert.Equal(ring == DartboardRing.Double, dart.IsDouble);
    }

    [Fact]
    public void BullsAndMiss_HaveFixedScores()
    {
        Assert.True(DartboardScoring.InnerBull.IsDouble);
        Assert.False(DartboardScoring.OuterBull.IsDouble);
        Assert.False(DartboardScoring.Miss.IsDouble);
    }

    [Fact]
    public void Sector_RejectsOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DartboardScoring.Sector(DartboardRing.Single, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DartboardScoring.Sector(DartboardRing.Single, 21));
    }

    [Fact]
    public void Visit_SumsThreeDartsAndRejectsFourth()
    {
        var visit = new DartboardVisit();

        Assert.True(visit.TryAdd(DartboardScoring.Sector(DartboardRing.Triple, 20)));
        Assert.True(visit.TryAdd(DartboardScoring.Sector(DartboardRing.Triple, 20)));
        Assert.True(visit.TryAdd(DartboardScoring.Sector(DartboardRing.Triple, 20)));

        Assert.Equal(180, visit.Sum);
        Assert.True(visit.IsFull);
        Assert.False(visit.TryAdd(DartboardScoring.Miss));
    }

    [Fact]
    public void Visit_UndoRemovesLastDart()
    {
        var visit = new DartboardVisit();
        visit.TryAdd(DartboardScoring.Sector(DartboardRing.Single, 20));
        visit.TryAdd(DartboardScoring.Sector(DartboardRing.Single, 5));

        Assert.True(visit.TryUndo());
        Assert.Equal(20, visit.Sum);
        Assert.Equal("x1 20", visit.Darts.Single().Label);
        Assert.False(visit.IsFull);
    }

    [Fact]
    public void Visit_TakeSumClearsDarts()
    {
        var visit = new DartboardVisit();
        visit.TryAdd(DartboardScoring.InnerBull);
        visit.TryAdd(DartboardScoring.Sector(DartboardRing.Double, 16));

        Assert.Equal(82, visit.TakeSum());
        Assert.Empty(visit.Darts);
        Assert.Equal(0, visit.Sum);
    }

    [Fact]
    public void Visit_BustReturnsZeroAndClears()
    {
        var visit = new DartboardVisit();
        visit.TryAdd(DartboardScoring.Sector(DartboardRing.Single, 20));

        Assert.Equal(0, visit.Bust());
        Assert.Empty(visit.Darts);
    }
}
