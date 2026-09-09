using Bunit;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Reports;
using DartsLeaderboard.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace DartsLeaderboard.Web.Tests;

public class WeeklyHighlightsPanelTests : BunitContext, IAsyncLifetime
{
    public WeeklyHighlightsPanelTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    [Fact]
    public void ShowsTitlesAndHolder()
    {
        var rows = WeeklyHighlights.From([], []).ToList();
        var index = rows.FindIndex(row => row.Key == WeeklyHighlights.MostWinsKey);
        rows[index] = rows[index] with { HolderName = "Анна", Value = "2" };

        var panel = Render<WeeklyHighlightsPanel>(parameters => parameters
            .Add(p => p.Items, rows));

        Assert.Contains("наныл недели", panel.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("за ваше здоровье...", panel.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("...и за мое очко", panel.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Анна", panel.Markup, StringComparison.Ordinal);
        Assert.Contains("2", panel.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(panel.FindAll("[data-testid=week-stat-most-wins]"));
        Assert.NotEmpty(panel.FindComponents<MudTable<WeeklyHighlightDto>>());
        Assert.Contains("Номинация", panel.Markup, StringComparison.Ordinal);
        Assert.Contains("Значение", panel.Markup, StringComparison.Ordinal);
        Assert.Contains("Игрок", panel.Markup, StringComparison.Ordinal);
        Assert.Equal(15, panel.FindAll("[data-testid^=week-stat-]").Count);
        var tooltips = panel.FindComponents<MudTooltip>();
        Assert.Equal(15, tooltips.Count);
        Assert.Equal(WeeklyHighlights.MostWinsDescription, tooltips[0].Instance.Text);
        Assert.Equal(WeeklyHighlights.Most21Description, tooltips[10].Instance.Text);
    }
}
