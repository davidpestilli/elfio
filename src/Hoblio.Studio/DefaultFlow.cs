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
            Start = "",
            Nodes = [],
            Edges = [],
        },
        Variables = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal),
    };
}
