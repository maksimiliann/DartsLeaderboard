using Bunit;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Web.Components.Match;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace DartsLeaderboard.Web.Tests;

public class DartboardInputTests : BunitContext, IAsyncLifetime
{
    public DartboardInputTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    private static int Sum(IReadOnlyList<ThrowDartDto>? darts) =>
        darts?.Sum(dart => dart.Points) ?? 0;

    [Fact]
    public void EmptyVisit_HidesDartChipsAndPointsLabel()
    {
        var component = Render<DartboardInput>();

        Assert.DoesNotContain("Дротик 1", component.Markup);
        Assert.DoesNotContain("Дротик 2", component.Markup);
        Assert.DoesNotContain("Дротик 3", component.Markup);
        Assert.DoesNotContain("Очки", component.Markup);
        Assert.DoesNotContain("Записать бросок", component.Markup);
        Assert.Empty(component.FindAll("[data-testid=submit-visit]"));
        Assert.Empty(component.FindAll(".dartboard-darts"));
        Assert.Empty(component.FindAll("[data-testid=visit-remaining]"));
        Assert.Contains("Сумма", component.Markup);
    }

    [Fact]
    public void Header_ShowsPlayerNameWithoutPointsLabel()
    {
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.PlayerName, "вася")
            .Add(p => p.Remaining, 101));

        Assert.Contains("вася", component.Markup);
        Assert.DoesNotContain("Очки", component.Markup);
        Assert.DoesNotContain("Дротик 1", component.Markup);
    }

    [Fact]
    public void SectorThenDouble_ScoresChosenDoubleWithoutCountingTheSectorTap()
    {
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.Remaining, 101));

        component.Find("[data-sector=16]").Click();

        Assert.Equal("Остаток 101", component.Find("[data-testid=visit-remaining]").TextContent.Trim());
        Assert.Empty(component.FindAll("[data-hit]"));

        component.Find("[data-testid=ring-double]").Click();

        Assert.Equal("Остаток 69", component.Find("[data-testid=visit-remaining]").TextContent.Trim());
        Assert.Single(component.FindAll("[data-hit]"));
    }

    [Fact]
    public void PreciseInput_IgnoresSectorTapAndScoresTheRing()
    {
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.Remaining, 101)
            .AddCascadingValue("SectorInput", false));

        component.Find("[data-sector=16]").Click();

        Assert.Equal("Остаток 101", component.Find("[data-testid=visit-remaining]").TextContent.Trim());
        Assert.Empty(component.FindAll("[data-testid=ring-double]"));

        component.Find("[data-dart=x2-16]").Click();

        Assert.Equal("Остаток 69", component.Find("[data-testid=visit-remaining]").TextContent.Trim());
    }

    [Fact]
    public void RemainingChip_UpdatesAfterEachDart()
    {
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.Remaining, 101));

        Assert.Equal("Остаток 101", component.Find("[data-testid=visit-remaining]").TextContent.Trim());

        component.Find("[data-dart=x1i-20]").Click();

        Assert.Equal("Остаток 81", component.Find("[data-testid=visit-remaining]").TextContent.Trim());
    }

    [Fact]
    public void ClickedSector_ShowsHitMarkerOnBoard()
    {
        var component = Render<DartboardInput>();

        Assert.Empty(component.FindAll("[data-hit]"));

        component.Find("[data-dart=x3-20]").Click();

        Assert.Single(component.FindAll("[data-hit]"));
        Assert.Equal("1", component.Find("[data-hit]").GetAttribute("data-hit"));

        component.Find("[data-dart=x3-20]").Click();
        component.Find("[data-dart=x3-20]").Click();

        Assert.Empty(component.FindAll("[data-hit]"));
    }

    [Fact]
    public void ThreeTripleTwenties_SubmitVisitSum()
    {
        IReadOnlyList<ThrowDartDto>? submitted = null;
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.OnSubmit, darts => submitted = darts));

        component.Find("[data-dart=x3-20]").Click();
        component.Find("[data-dart=x3-20]").Click();
        component.Find("[data-dart=x3-20]").Click();

        Assert.Equal(180, Sum(submitted));
    }

    [Fact]
    public void DoubleSixteen_CheckoutsWithoutASubmitButton()
    {
        IReadOnlyList<ThrowDartDto>? submitted = null;
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.Remaining, 32)
            .Add(p => p.OnSubmit, darts => submitted = darts));

        component.Find("[data-dart=x2-16]").Click();

        Assert.Equal(32, Sum(submitted));
        Assert.True(submitted!.Single().IsDouble);
    }

    [Fact]
    public void NumberRing_CountsAsMiss()
    {
        IReadOnlyList<ThrowDartDto>? submitted = null;
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.OnSubmit, darts => submitted = darts));

        component.Find("[data-dart=miss-20]").Click();
        Assert.Null(submitted);

        component.Find("[data-dart=miss-20]").Click();
        component.Find("[data-dart=miss-20]").Click();

        Assert.Equal(0, Sum(submitted));
        Assert.Equal(3, submitted!.Count);
        Assert.All(submitted, dart => Assert.False(dart.IsDouble));
    }

    [Fact]
    public void RemainingExceededOnFirstDart_SubmitsZero()
    {
        IReadOnlyList<ThrowDartDto>? submitted = null;
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.Remaining, 50)
            .Add(p => p.OnSubmit, darts => submitted = darts));

        component.Find("[data-dart=x3-20]").Click();

        Assert.Equal(0, Sum(submitted));
    }

    [Fact]
    public void VisitWouldLeaveOne_SubmitsZero()
    {
        IReadOnlyList<ThrowDartDto>? submitted = null;
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.Remaining, 21)
            .Add(p => p.OnSubmit, darts => submitted = darts));

        component.Find("[data-dart=x1i-20]").Click();

        Assert.Equal(0, Sum(submitted));
    }

    [Fact]
    public void SingleOnRemaining_SubmitsZero()
    {
        IReadOnlyList<ThrowDartDto>? submitted = null;
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.Remaining, 20)
            .Add(p => p.OnSubmit, darts => submitted = darts));

        component.Find("[data-dart=x1i-20]").Click();

        Assert.Equal(0, Sum(submitted));
    }

    [Fact]
    public void DoubleOnFirstDart_CheckoutsImmediately()
    {
        IReadOnlyList<ThrowDartDto>? submitted = null;
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.Remaining, 40)
            .Add(p => p.OnSubmit, darts => submitted = darts));

        component.Find("[data-dart=x2-20]").Click();

        Assert.Equal(40, Sum(submitted));
        Assert.True(submitted!.Single().IsDouble);
    }

    [Fact]
    public void SingleThenDouble_Checkouts()
    {
        IReadOnlyList<ThrowDartDto>? submitted = null;
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.Remaining, 40)
            .Add(p => p.OnSubmit, darts => submitted = darts));

        component.Find("[data-dart=x1i-20]").Click();
        component.Find("[data-dart=x2-10]").Click();

        Assert.Equal(40, Sum(submitted));
        Assert.False(submitted![0].IsDouble);
        Assert.True(submitted[1].IsDouble);
    }
}
