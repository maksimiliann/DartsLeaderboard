using Bunit;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Web.Components.Match;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace DartsLeaderboard.Web.Tests;

public class CurrentRoundStripTests : BunitContext, IAsyncLifetime
{
    public CurrentRoundStripTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    [Fact]
    public void CurrentRound_ShowsEachPlayerOnOneLineWithoutEarlierRoundsOrRemaining()
    {
        var component = Render<CurrentRoundStrip>(parameters => parameters
            .Add(p => p.State, RoundTwo()));

        var strip = component.Find("[data-testid=current-round]");
        Assert.Equal("2", strip.QuerySelector("[data-testid=round-number]")!.TextContent.Trim());

        var cells = strip.QuerySelectorAll("[data-testid=round-player]");
        Assert.Equal(3, cells.Length);

        Assert.Contains("вася", cells[0].TextContent);
        Assert.Contains("63", cells[0].TextContent);
        Assert.DoesNotContain("is-current", cells[0].ClassName);

        Assert.Contains("петя", cells[1].TextContent);
        Assert.Contains("ходит", cells[1].TextContent);
        Assert.Contains("is-current", cells[1].ClassName);

        Assert.Contains("максим", cells[2].TextContent);
        Assert.Contains("82", cells[2].TextContent);
        Assert.Contains("is-previous", cells[2].QuerySelector(".round-score")!.ClassName);
        Assert.DoesNotContain("—", cells[2].TextContent);
        Assert.DoesNotContain("is-current", cells[2].ClassName);

        Assert.DoesNotContain("31", strip.TextContent);
        Assert.DoesNotContain("94", strip.TextContent);
        Assert.DoesNotContain("Осталось", strip.TextContent);
        Assert.DoesNotContain("ост.", strip.TextContent);
    }

    [Fact]
    public void FirstRound_KeepsDashWhenThereIsNoPreviousScore()
    {
        var component = Render<CurrentRoundStrip>(parameters => parameters
            .Add(p => p.State, RoundTwo() with
            {
                CurrentRoundNumber = 1,
                CurrentParticipantId = 1,
                CurrentPlayerName = "вася",
                Rows = [new MatchRowDto(1, [new MatchCellDto(null, null), new MatchCellDto(null, null), new MatchCellDto(null, null)])]
            }));

        var cells = component.FindAll("[data-testid=round-player]");
        Assert.Contains("ходит", cells[0].TextContent);
        Assert.Contains("—", cells[1].TextContent);
        Assert.Contains("—", cells[2].TextContent);
        Assert.DoesNotContain("is-previous", component.Markup);
    }

    private static MatchStateDto RoundTwo() => new(
        MatchId: 58,
        ModeTitle: "101 на очки",
        StatusTitle: "Идёт",
        IsInProgress: true,
        CurrentRoundNumber: 2,
        CurrentParticipantId: 2,
        CurrentPlayerName: "петя",
        WinnerPlayerName: null,
        Columns:
        [
            new MatchColumnDto(1, "вася", []),
            new MatchColumnDto(2, "петя", []),
            new MatchColumnDto(3, "максим", [])
        ],
        Rows:
        [
            new MatchRowDto(1, [new MatchCellDto(31, null), new MatchCellDto(94, null), new MatchCellDto(82, null)]),
            new MatchRowDto(2, [new MatchCellDto(63, null), new MatchCellDto(null, null), new MatchCellDto(null, null)])
        ],
        MatchStatistics: [],
        ShowTrendChart: false,
        ChartSeries: [],
        Standings: [],
        CurrentRemaining: 7);
}
