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
    public void EmptyVisit_ShowsDartSlotsOneThroughThree()
    {
        var component = Render<DartboardInput>();

        Assert.Contains("Дротик 1", component.Markup);
        Assert.Contains("Дротик 2", component.Markup);
        Assert.Contains("Дротик 3", component.Markup);
        Assert.DoesNotContain("Дротик 4", component.Markup);
    }

    [Fact]
    public void ClickedSector_ShowsHitMarkerOnBoard()
    {
        var component = Render<DartboardInput>();

        Assert.Empty(component.FindAll("[data-hit]"));

        component.Find("[data-dart=x3-20]").Click();

        Assert.Single(component.FindAll("[data-hit]"));
        Assert.Equal("1", component.Find("[data-hit]").GetAttribute("data-hit"));

        component.Find("[data-testid=submit-visit]").Click();

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
    public void DoubleSixteenThenSubmit_SendsThirtyTwo()
    {
        IReadOnlyList<ThrowDartDto>? submitted = null;
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.OnSubmit, darts => submitted = darts));

        component.Find("[data-dart=x2-16]").Click();
        component.Find("[data-testid=submit-visit]").Click();

        Assert.Equal(32, Sum(submitted));
        Assert.True(submitted!.Single().IsDouble);
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

        component.Find("[data-dart=x1-20]").Click();

        Assert.Equal(0, Sum(submitted));
    }

    [Fact]
    public void SingleOnRemaining_SubmitsZero()
    {
        IReadOnlyList<ThrowDartDto>? submitted = null;
        var component = Render<DartboardInput>(parameters => parameters
            .Add(p => p.Remaining, 20)
            .Add(p => p.OnSubmit, darts => submitted = darts));

        component.Find("[data-dart=x1-20]").Click();

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

        component.Find("[data-dart=x1-20]").Click();
        component.Find("[data-dart=x2-10]").Click();

        Assert.Equal(40, Sum(submitted));
        Assert.False(submitted![0].IsDouble);
        Assert.True(submitted[1].IsDouble);
    }
}
