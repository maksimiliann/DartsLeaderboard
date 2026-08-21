using DartsLeaderboard.Application.Contracts;
using Microsoft.AspNetCore.Components;

namespace DartsLeaderboard.Web.Components.Match;

public partial class MatchScoreGrid
{
    [Parameter, EditorRequired] public MatchStateDto State { get; set; } = default!;

    private IEnumerable<string> StatisticNames =>
        State.Columns.SelectMany(c => c.Statistics.Select(s => s.Name)).Distinct();

    private bool UseThrowHeat => State.ShowTrendChart;

    private (int Min, int Max)? ThrowRange
    {
        get
        {
            var values = State.Rows
                .SelectMany(row => row.Cells)
                .Where(cell => cell.Points is not null)
                .Select(cell => cell.Points!.Value)
                .ToList();
            if (values.Count == 0)
            {
                return null;
            }

            return (values.Min(), values.Max());
        }
    }

    private static bool IsEmphasized(string name) =>
        name is "Сумма" or "Осталось";

    private string? CellStyle(MatchCellDto cell, bool isCurrent)
    {
        if (cell.Points is null || !UseThrowHeat || ThrowRange is not { } range)
        {
            return isCurrent ? null : "text-align: center";
        }

        var heat = ThrowHeatColor.For(cell.Points.Value, range.Min, range.Max);
        return $"text-align: center; background-color: {heat}; color: #fff";
    }
}
