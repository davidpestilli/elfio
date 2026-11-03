using System.Text.Json;

namespace Hoblio.Model;

public sealed class SubflowBundle
{
    public required string Start { get; set; }
    public required List<Node> Nodes { get; set; }
    public required List<Edge> Edges { get; set; }
    public Dictionary<string, JsonElement> Variables { get; set; } = new(StringComparer.Ordinal);

    public FlowGraph AsGraph(IReadOnlyDictionary<string, SubflowBundle>? siblingSubflows)
    {
        Dictionary<string, SubflowBundle>? subflows = null;
        if (siblingSubflows is { Count: > 0 })
            subflows = new Dictionary<string, SubflowBundle>(siblingSubflows, StringComparer.Ordinal);
        return new FlowGraph
        {
            Start = Start,
            Nodes = Nodes,
            Edges = Edges,
            Subflows = subflows,
        };
    }
}
