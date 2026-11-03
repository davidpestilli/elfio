using System.Text.Json;
using Hoblio.Model;
using Hoblio.Model.Json;

namespace Hoblio.Serde;

public sealed class LoadedFlowFile
{
    public required FlowGraph Graph { get; init; }
    public Dictionary<string, JsonElement> Variables { get; init; } = new(StringComparer.Ordinal);
    public required string Path { get; init; }
}

public sealed class ResolvedChildFlow
{
    public required string Label { get; init; }
    public required FlowGraph Graph { get; init; }
    public Dictionary<string, JsonElement> Variables { get; init; } = new(StringComparer.Ordinal);
}

public static class ChildFlowResolver
{
    public const int MaxRunFlowDepth = 16;

    public static ResolvedChildFlow Resolve(
        string? projectRoot,
        FlowManifest manifest,
        FlowGraph parentGraph,
        RunFlowAction action,
        Dictionary<string, LoadedFlowFile> cache)
    {
        if (action is null)
            throw new InvalidOperationException("not a run_flow action");

        if (!string.IsNullOrWhiteSpace(action.FlowId))
        {
            var id = action.FlowId.Trim();
            if (parentGraph.Subflows is not null && parentGraph.Subflows.TryGetValue(id, out var bundle))
            {
                return new ResolvedChildFlow
                {
                    Label = $"subflow:{id}",
                    Graph = bundle.AsGraph(parentGraph.Subflows),
                    Variables = new Dictionary<string, JsonElement>(bundle.Variables, StringComparer.Ordinal),
                };
            }

            if (manifest.Flows is not null && manifest.Flows.TryGetValue(id, out var entry))
            {
                if (string.IsNullOrWhiteSpace(projectRoot))
                    throw new InvalidOperationException($"run_flow flow_id `{id}` requires project root");
                var loaded = LoadFlowCached(projectRoot, entry.Path, cache);
                return new ResolvedChildFlow
                {
                    Label = id,
                    Graph = loaded.Graph,
                    Variables = loaded.Variables,
                };
            }

            throw new InvalidOperationException($"run_flow flow_id `{id}` not found in subflows or manifest.flows");
        }

        if (!string.IsNullOrWhiteSpace(action.Path))
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("run_flow path requires project root");
            var loaded = LoadFlowCached(projectRoot, action.Path.Trim(), cache);
            return new ResolvedChildFlow
            {
                Label = loaded.Path,
                Graph = loaded.Graph,
                Variables = loaded.Variables,
            };
        }

        throw new InvalidOperationException("run_flow requires flow_id or path");
    }

    public static LoadedFlowFile LoadFlowCached(string projectRoot, string relativePath, Dictionary<string, LoadedFlowFile> cache)
    {
        var full = ResolveProjectFlowPath(projectRoot, relativePath);
        var key = full.ToLowerInvariant();
        if (cache.TryGetValue(key, out var cached))
            return cached;
        var loaded = LoadFlowFile(full);
        cache[key] = loaded;
        return loaded;
    }

    public static string ResolveProjectFlowPath(string projectRoot, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative))
            throw new InvalidOperationException("flow path is empty");
        if (Path.IsPathRooted(relative))
            throw new InvalidOperationException("flow path must be relative to the project root");
        var joined = Path.GetFullPath(Path.Combine(projectRoot, relative));
        var root = Path.GetFullPath(projectRoot);
        if (!joined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"flow path `{relative}` resolves outside the project root");
        return joined;
    }

    public static LoadedFlowFile LoadFlowFile(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"flow file not found: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var graph = doc.RootElement.Deserialize<FlowGraph>(HoblioJson.Options)
            ?? throw new InvalidOperationException($"invalid flow schema in {path}");
        Dictionary<string, JsonElement> variables = new(StringComparer.Ordinal);
        if (doc.RootElement.TryGetProperty("variables", out var varsEl) && varsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in varsEl.EnumerateObject())
                variables[prop.Name] = prop.Value.Clone();
        }
        return new LoadedFlowFile { Graph = graph, Variables = variables, Path = path };
    }
}
