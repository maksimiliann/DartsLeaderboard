using Bunit;
using DartsLeaderboard.Web.Components.Match;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace DartsLeaderboard.Web.Tests;

public class RoundInputTests : BunitContext, IAsyncLifetime
{
    public RoundInputTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    [Fact]
    public void EmptyValue_SubmitsZero()
    {
        int? submitted = null;
        var component = Render<RoundInput>(parameters => parameters
            .Add(p => p.OnSubmit, points => submitted = points));

        component.Find("input").KeyDown(key: "Enter");

        Assert.Equal(0, submitted);
    }

    [Fact]
    public void EnteredValue_SubmitsPointsAndClearsField()
    {
        int? submitted = null;
        var component = Render<RoundInput>(parameters => parameters
            .Add(p => p.OnSubmit, points => submitted = points));

        var input = component.Find("input");
        input.Input("60");
        input.KeyDown(key: "Enter");

        Assert.Equal(60, submitted);
        Assert.Equal(string.Empty, component.Find("input").GetAttribute("value") ?? string.Empty);
    }
}
