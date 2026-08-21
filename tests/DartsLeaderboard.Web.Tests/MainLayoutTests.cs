using Bunit;
using DartsLeaderboard.Web.Components.Layout;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace DartsLeaderboard.Web.Tests;

public class MainLayoutTests : BunitContext, IAsyncLifetime
{
    public MainLayoutTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    [Fact]
    public void StartsInLightMode()
    {
        var layout = RenderLayout();

        var toggle = layout.Find("[data-testid=theme-toggle]");
        Assert.Equal("Включить тёмную тему", toggle.GetAttribute("aria-label"));
    }

    [Fact]
    public void Toggle_SwitchesToDarkThenBackToLight()
    {
        var layout = RenderLayout();

        var toggle = layout.Find("[data-testid=theme-toggle]");
        toggle.Click();

        Assert.Equal("Включить светлую тему", toggle.GetAttribute("aria-label"));

        toggle.Click();

        Assert.Equal("Включить тёмную тему", toggle.GetAttribute("aria-label"));
    }

    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, builder => builder.AddMarkupContent(0, "<div>ok</div>")));
}
