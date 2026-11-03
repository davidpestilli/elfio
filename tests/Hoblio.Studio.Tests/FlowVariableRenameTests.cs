using Hoblio.Model;
using Hoblio.Studio;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class FlowVariableRenameTests
{
    [Fact]
    public void ApplyReferences_updates_templates_and_consumer_fields()
    {
        var nodes = new List<Node>
        {
            new()
            {
                Id = "set1",
                Kind = NodeKind.Action,
                Action = new SetVariableAction { Variable = "foo", Value = "1" },
            },
            new()
            {
                Id = "log1",
                Kind = NodeKind.Action,
                Action = new LogAction { Message = "hello {foo}" },
            },
            new()
            {
                Id = "inc1",
                Kind = NodeKind.Action,
                Action = new IncreaseVariableAction { Variable = "foo" },
            },
        };

        var n = FlowVariableRename.ApplyReferences(nodes, "foo", "bar");
        Assert.True(n >= 2);
        Assert.Equal("bar", ((SetVariableAction)nodes[0].Action!).Variable);
        Assert.Contains("{bar}", ((LogAction)nodes[1].Action!).Message);
        Assert.Equal("bar", ((IncreaseVariableAction)nodes[2].Action!).Variable);
    }

    [Fact]
    public void WouldDuplicateProducer_detects_other_node()
    {
        var nodes = new List<Node>
        {
            new() { Id = "a", Kind = NodeKind.Action, Action = new SetVariableAction { Variable = "x", Value = "1" } },
            new() { Id = "b", Kind = NodeKind.Action, Action = new SetVariableAction { Variable = "y", Value = "2" } },
        };
        Assert.True(FlowVariableRename.WouldDuplicateProducer(nodes, "y", "a"));
        Assert.False(FlowVariableRename.WouldDuplicateProducer(nodes, "y", "b"));
    }
}
