using Hoblio.Model;
using Hoblio.Studio;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class FlowProducerValidationTests
{
    [Fact]
    public void Rejects_duplicate_producer()
    {
        var nodes = new List<Node>
        {
            new() { Id = "a", Kind = NodeKind.Action, Action = new SetVariableAction { Variable = "x", Value = "1" } },
        };
        var err = FlowProducerValidation.ValidateProducerName(nodes, "x", "b");
        Assert.NotNull(err);
    }

    [Fact]
    public void Allows_same_node_keep_name()
    {
        var nodes = new List<Node>
        {
            new() { Id = "a", Kind = NodeKind.Action, Action = new SetVariableAction { Variable = "x", Value = "1" } },
        };
        Assert.Null(FlowProducerValidation.ValidateProducerName(nodes, "x", "a"));
    }
}
