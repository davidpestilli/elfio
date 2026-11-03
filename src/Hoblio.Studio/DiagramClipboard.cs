using System.Text.Json;
using Hoblio.Model;
using Hoblio.Model.Json;

namespace Hoblio.Studio;

public sealed record DiagramClipboardPayload(List<Node> Nodes, List<Edge> Edges);

public static class DiagramClipboard
{
    public const string ClipboardPrefix = "HOBLIO_DIAGRAM_V1:";

    public static string Serialize(DiagramClipboardPayload payload) =>
        ClipboardPrefix + JsonSerializer.Serialize(payload, HoblioJson.Options);

    public static bool TryDeserialize(string? text, out DiagramClipboardPayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith(ClipboardPrefix, StringComparison.Ordinal))
            return false;
        try
        {
            payload = JsonSerializer.Deserialize<DiagramClipboardPayload>(
                text[ClipboardPrefix.Length..],
                HoblioJson.Options);
            return payload is { Nodes.Count: > 0 };
        }
        catch
        {
            return false;
        }
    }

    public static DiagramClipboardPayload ExtractSelection(IReadOnlyList<Node> allNodes, IReadOnlyList<Edge> allEdges, IReadOnlyCollection<string> selectedNodeIds)
    {
        var idSet = selectedNodeIds.ToHashSet(StringComparer.Ordinal);
        var nodes = allNodes.Where(n => idSet.Contains(n.Id)).ToList();
        var edges = allEdges
            .Where(e => idSet.Contains(e.From) && idSet.Contains(e.To))
            .ToList();
        return new DiagramClipboardPayload(nodes, edges);
    }

    public static (List<Node> Nodes, List<Edge> Edges) PasteWithNewIds(
        DiagramClipboardPayload payload,
        bool graphAlreadyHasStart,
        double offsetX,
        double offsetY)
    {
        var idMap = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var node in payload.Nodes)
        {
            if (graphAlreadyHasStart && node.Kind == NodeKind.Start)
                continue;
            idMap[node.Id] = NewNodeId(node.Kind);
        }

        var pastedNodes = new List<Node>();
        foreach (var node in payload.Nodes)
        {
            if (!idMap.TryGetValue(node.Id, out var newId))
                continue;
            pastedNodes.Add(CloneNode(node, newId, offsetX, offsetY));
        }

        var pastedEdges = new List<Edge>();
        foreach (var edge in payload.Edges)
        {
            if (!idMap.TryGetValue(edge.From, out var from) || !idMap.TryGetValue(edge.To, out var to))
                continue;
            pastedEdges.Add(CloneEdge(edge, from, to));
        }

        return (pastedNodes, pastedEdges);
    }

    static string NewNodeId(NodeKind kind) => kind switch
    {
        NodeKind.Start => $"start_{Guid.NewGuid():N}"[..12],
        NodeKind.End => $"end_{Guid.NewGuid():N}"[..12],
        _ => $"node_{Guid.NewGuid():N}"[..12],
    };

    static Node CloneNode(Node source, string newId, double dx, double dy)
    {
        var json = JsonSerializer.Serialize(source, HoblioJson.Options);
        using var doc = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.NameEquals("id"))
                {
                    writer.WriteString("id", newId);
                    continue;
                }

                if (prop.NameEquals("ui") && prop.Value.ValueKind == JsonValueKind.Object)
                {
                    writer.WritePropertyName("ui");
                    writer.WriteStartObject();
                    foreach (var uiProp in prop.Value.EnumerateObject())
                    {
                        if (uiProp.NameEquals("x") && uiProp.Value.TryGetDouble(out var x))
                            writer.WriteNumber("x", x + dx);
                        else if (uiProp.NameEquals("y") && uiProp.Value.TryGetDouble(out var y))
                            writer.WriteNumber("y", y + dy);
                        else
                            uiProp.WriteTo(writer);
                    }
                    writer.WriteEndObject();
                    continue;
                }

                prop.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        return JsonSerializer.Deserialize<Node>(stream.ToArray(), HoblioJson.Options)
               ?? throw new InvalidOperationException("clone node failed");
    }

    static Edge CloneEdge(Edge source, string from, string to)
    {
        var json = JsonSerializer.Serialize(source, HoblioJson.Options);
        using var doc = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.NameEquals("from"))
                {
                    writer.WriteString("from", from);
                    continue;
                }
                if (prop.NameEquals("to"))
                {
                    writer.WriteString("to", to);
                    continue;
                }
                if (prop.NameEquals("id"))
                {
                    writer.WriteString("id", $"edge_{Guid.NewGuid():N}"[..12]);
                    continue;
                }
                prop.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        return JsonSerializer.Deserialize<Edge>(stream.ToArray(), HoblioJson.Options)
               ?? throw new InvalidOperationException("clone edge failed");
    }
}
