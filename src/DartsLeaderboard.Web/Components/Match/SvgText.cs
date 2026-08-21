using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace DartsLeaderboard.Web.Components.Match;

public sealed class SvgText : ComponentBase
{
    [Parameter] public string? Class { get; set; }

    [Parameter] public string? X { get; set; }

    [Parameter] public string? Y { get; set; }

    [Parameter] public string TextAnchor { get; set; } = "middle";

    [Parameter] public string DominantBaseline { get; set; } = "middle";

    [Parameter] public string? Fill { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "class", Class);
        builder.AddAttribute(2, "x", X);
        builder.AddAttribute(3, "y", Y);
        builder.AddAttribute(4, "text-anchor", TextAnchor);
        builder.AddAttribute(5, "dominant-baseline", DominantBaseline);
        if (Fill is not null)
        {
            builder.AddAttribute(6, "fill", Fill);
        }

        builder.AddContent(7, ChildContent);
        builder.CloseElement();
    }
}
