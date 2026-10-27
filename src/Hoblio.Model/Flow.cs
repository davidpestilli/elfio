using System.Text.Json.Serialization;

namespace Hoblio.Model;

public sealed class FlowGraph
{
    public required string Start { get; set; }
    public required List<Node> Nodes { get; set; }
    public required List<Edge> Edges { get; set; }
}

public sealed class Node
{
    public required string Id { get; init; }
    public required NodeKind Kind { get; init; }
    public string? Label { get; set; }
    public ActionKind? Action { get; set; }
    public ControlKind? Control { get; set; }
    public NodeUi? Ui { get; set; }
    public ErrorHandling? ErrorHandling { get; set; }
}

public sealed class NodeUi
{
    public double X { get; init; }
    public double Y { get; init; }
}

public sealed class Edge
{
    public string? Id { get; init; }
    public required string From { get; init; }
    public required string To { get; init; }
    public string? Label { get; init; }
    public string? Port { get; init; }
    public EdgeUi? Ui { get; init; }

    public EdgePort ResolvedPort()
    {
        if (Port is { } p && EdgePortExtensions.Parse(p) is { } ep)
            return ep;
        if (Ui?.SourceHandle is { } h && EdgePortExtensions.Parse(h) is { } ep2)
            return ep2;
        return EdgePort.Main;
    }
}

public sealed class EdgeUi
{
    public List<Waypoint>? Waypoints { get; init; }

    [JsonPropertyName("sourceHandle")]
    public string? SourceHandle { get; init; }

    [JsonPropertyName("targetHandle")]
    public string? TargetHandle { get; init; }
}

public sealed class Waypoint
{
    public double X { get; init; }
    public double Y { get; init; }
}
