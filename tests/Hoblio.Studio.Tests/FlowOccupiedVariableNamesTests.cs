using Hoblio.Model;
using Hoblio.Studio;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class FlowOccupiedVariableNamesTests
{
    [Fact]
    public void Collect_includes_actions_loops_and_seeds()
    {
        var nodes = new List<Node>
        {
            new() { Id = "s", Kind = NodeKind.Action, Action = new SetVariableAction { Variable = "Var1", Value = "1" } },
            new()
            {
                Id = "f",
                Kind = NodeKind.ForEach,
                Control = new ForEachControlKind { ListVariable = "items", ItemVariable = "item" },
            },
        };
        var set = FlowOccupiedVariableNames.Collect(nodes, ["seed"]);
        Assert.Contains("Var1", set);
        Assert.Contains("item", set);
        Assert.Contains("seed", set);
    }
}
