using Bunit;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Web.Components.Match;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace DartsLeaderboard.Web.Tests;

public class MatchStandingsSpeechTests : BunitContext, IAsyncLifetime
{
    public MatchStandingsSpeechTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    [Fact]
    public void SpeakButton_HiddenWhileMatchIsInProgress()
    {
        var panel = Render<MatchStandings>(parameters => parameters
            .Add(p => p.Standings, [new StandingDto(1, "Иван", 120)]));

        Assert.Empty(panel.FindAll("[data-testid=speak-results]"));
    }

    [Fact]
    public void SpeakButton_ShownForFinalTable()
    {
        var panel = Render<MatchStandings>(parameters => parameters
            .Add(p => p.Standings, [new StandingDto(1, "Иван", 120)])
            .Add(p => p.ShowSpeakButton, true));

        Assert.Contains("Озвучить результат", panel.Find("[data-testid=speak-results]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void SpeakButton_DisabledWhileSpeaking()
    {
        var panel = Render<MatchStandings>(parameters => parameters
            .Add(p => p.Standings, [new StandingDto(1, "Иван", 120)])
            .Add(p => p.ShowSpeakButton, true)
            .Add(p => p.Speaking, true));

        Assert.NotNull(panel.Find("[data-testid=speak-results]").GetAttribute("disabled"));
    }

    [Theory]
    [InlineData("Пауза")]
    [InlineData("Продолжить")]
    public void SpeakButton_ShowsPlaybackLabel(string label)
    {
        var panel = Render<MatchStandings>(parameters => parameters
            .Add(p => p.Standings, [new StandingDto(1, "Иван", 120)])
            .Add(p => p.ShowSpeakButton, true)
            .Add(p => p.SpeakLabel, label));

        Assert.Contains(label, panel.Find("[data-testid=speak-results]").TextContent, StringComparison.Ordinal);
    }
}
