using Hoblio.Model;

namespace Hoblio.Model.Tests;

public class GraphValidatorTests
{
    static FlowGraph DemoGraph()
    {
        return new FlowGraph
        {
            Start = "start",
            Nodes =
            [
                new Node { Id = "start", Kind = NodeKind.Start, Label = "Start" },
                new Node { Id = "log1", Kind = NodeKind.Action, Label = "Log", Action = new LogAction { Message = "a" } },
                new Node { Id = "end", Kind = NodeKind.End, Label = "End" },
            ],
            Edges =
            [
                new Edge { From = "start", To = "log1" },
                new Edge { From = "log1", To = "end" },
            ],
        };
    }

    [Fact]
    public void Valid_demo_graph()
    {
        GraphValidator.ValidateGraph(DemoGraph());
    }

    [Fact]
    public void If_requires_then_and_else()
    {
        var baseNodes = DemoGraph().Nodes.ToList();
        baseNodes.Insert(2, new Node
        {
            Id = "cond",
            Kind = NodeKind.If,
            Control = new IfControlKind { Left = "flag", Operator = CompareOperator.Eq, Right = "yes" },
        });
        var g = new FlowGraph
        {
            Start = "start",
            Nodes = baseNodes,
            Edges =
            [
                new Edge { From = "start", To = "cond" },
                new Edge { From = "cond", To = "log1", Port = "then" },
            ],
        };
        var ex = Assert.Throws<ValidationException>(() => GraphValidator.ValidateGraph(g));
        Assert.Equal(ValidationErrorKind.MissingOutgoingPort, ex.Kind);
        Assert.Contains("else", ex.Message);
    }
}
