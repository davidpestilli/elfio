using Hoblio.Model;
using Hoblio.Studio;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class DiagramClipboardTests
{
    [Fact]
    public void Paste_assigns_new_ids_and_remaps_edges()
    {
        var payload = new DiagramClipboardPayload(
        [
            new Node { Id = "n1", Kind = NodeKind.Action, Label = "A", Action = new LogAction { Message = "x" }, Ui = new NodeUi { X = 10, Y = 20 } },
            new Node { Id = "n2", Kind = NodeKind.Action, Label = "B", Action = new LogAction { Message = "y" }, Ui = new NodeUi { X = 200, Y = 20 } },
        ],
        [new Edge { From = "n1", To = "n2", Port = "main" }]);

        var (nodes, edges) = DiagramClipboard.PasteWithNewIds(payload, graphAlreadyHasStart: false, 16, 16);
        Assert.Equal(2, nodes.Count);
        Assert.All(nodes, n => Assert.NotEqual("n1", n.Id));
        Assert.Single(edges);
        Assert.Equal(nodes[0].Id, edges[0].From);
        Assert.Equal(nodes[1].Id, edges[0].To);
        Assert.Equal(26, nodes[0].Ui!.X);
    }

    [Fact]
    public void Serialize_roundtrip()
    {
        var payload = new DiagramClipboardPayload(
            [new Node { Id = "n1", Kind = NodeKind.End, Label = "End" }],
            []);
        var text = DiagramClipboard.Serialize(payload);
        Assert.True(DiagramClipboard.TryDeserialize(text, out var back));
        Assert.Single(back!.Nodes);
    }
}
