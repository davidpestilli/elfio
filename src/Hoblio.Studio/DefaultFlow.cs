using Hoblio.Model;

namespace Hoblio.Studio;

public static class DefaultFlow
{
    public static FlowProject Create() => new()
    {
        Manifest = new FlowManifest
        {
            Name = "untitled",
            Version = "0.1.0",
            Description = "New Hoblio flow",
        },
        Flow = new FlowGraph
        {
            Start = "start",
            Nodes =
            [
                new Node { Id = "start", Kind = NodeKind.Start, Label = "Start", Ui = new NodeUi { X = 80, Y = 120 } },
                new Node { Id = "end", Kind = NodeKind.End, Label = "End", Ui = new NodeUi { X = 720, Y = 120 } },
            ],
            Edges = [new Edge { From = "start", To = "end" }],
        },
        Variables = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal),
    };
}
