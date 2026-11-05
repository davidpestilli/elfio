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

public sealed class LoadedProject
{
    public required FlowProject Project { get; init; }
    public required string Root { get; init; }
}

/// <summary>Cache de fluxos e projetos por run (evita reler JSON em loops).</summary>
public sealed class ChildFlowCache
{
    public Dictionary<string, LoadedFlowFile> Flows { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, LoadedProject> Projects { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ResolvedChildFlow
{
    public required string Label { get; init; }
    public required FlowGraph Graph { get; init; }
    public Dictionary<string, JsonElement> Variables { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Manifest que governa o fluxo filho (o do projeto vinculado, quando há <c>project</c>).</summary>
    public required FlowManifest Manifest { get; init; }

    /// <summary>Raiz usada para resolver caminhos relativos dentro do fluxo filho.</summary>
    public required string? ProjectRoot { get; init; }
}

public static class ChildFlowResolver
{
    public const int MaxRunFlowDepth = 16;

    public static ResolvedChildFlow Resolve(
        string? projectRoot,
        FlowManifest manifest,
        FlowGraph parentGraph,
        RunFlowAction action,
        ChildFlowCache cache)
    {
        if (action is null)
            throw new InvalidOperationException("not a run_flow action");

        if (!string.IsNullOrWhiteSpace(action.Project))
            return ResolveLinkedProject(projectRoot, manifest, action, cache);

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
                    Manifest = manifest,
                    ProjectRoot = projectRoot,
                };
            }

            if (manifest.Flows is not null && manifest.Flows.TryGetValue(id, out var entry))
            {
                if (string.IsNullOrWhiteSpace(projectRoot))
                    throw new InvalidOperationException($"run_flow flow_id `{id}` requires project root");
                var loaded = LoadFlowCached(projectRoot, entry.Path, cache.Flows);
                return new ResolvedChildFlow
                {
                    Label = id,
                    Graph = loaded.Graph,
                    Variables = loaded.Variables,
                    Manifest = manifest,
                    ProjectRoot = projectRoot,
                };
            }

            throw new InvalidOperationException($"run_flow flow_id `{id}` not found in subflows or manifest.flows");
        }

        if (!string.IsNullOrWhiteSpace(action.Path))
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("run_flow path requires project root");
            var loaded = LoadFlowCached(projectRoot, action.Path.Trim(), cache.Flows);
            return new ResolvedChildFlow
            {
                Label = loaded.Path,
                Graph = loaded.Graph,
                Variables = loaded.Variables,
                Manifest = manifest,
                ProjectRoot = projectRoot,
            };
        }

        throw new InvalidOperationException("run_flow requires flow_id or path");
    }

    /// <summary>
    /// Resolve um fluxo principal de outro projeto declarado em <c>manifest.linked_projects</c>.
    /// O projeto vinculado traz seu próprio manifest e sua própria raiz (caminhos relativos do filho).
    /// </summary>
    static ResolvedChildFlow ResolveLinkedProject(
        string? projectRoot,
        FlowManifest manifest,
        RunFlowAction action,
        ChildFlowCache cache)
    {
        var linkId = action.Project!.Trim();
        if (string.IsNullOrWhiteSpace(linkId))
            throw new InvalidOperationException("run_flow project is empty");

        if (manifest.LinkedProjects is null || !manifest.LinkedProjects.TryGetValue(linkId, out var link))
            throw new InvalidOperationException(
                $"run_flow project `{linkId}` is not declared in manifest.linked_projects");

        if (string.IsNullOrWhiteSpace(projectRoot))
            throw new InvalidOperationException($"run_flow project `{linkId}` requires project root");

        var targetRoot = ResolveLinkedProjectPath(projectRoot, link.Path, linkId);
        var loaded = LoadProjectCached(targetRoot, cache);
        var childManifest = loaded.Project.Manifest;

        var flowRef = !string.IsNullOrWhiteSpace(action.FlowId)
            ? action.FlowId.Trim()
            : (link.Flow ?? "").Trim();

        if (flowRef.Length == 0)
        {
            if (!string.IsNullOrWhiteSpace(action.Path))
            {
                var byPath = LoadFlowCached(targetRoot, action.Path.Trim(), cache.Flows);
                return new ResolvedChildFlow
                {
                    Label = $"{linkId}:{action.Path!.Trim()}",
                    Graph = byPath.Graph,
                    Variables = byPath.Variables,
                    Manifest = childManifest,
                    ProjectRoot = targetRoot,
                };
            }

            return ResolveEntryFlow(loaded, linkId, cache);
        }

        if (childManifest.Flows is null || !childManifest.Flows.TryGetValue(flowRef, out var entry))
            throw new InvalidOperationException(
                $"run_flow project `{linkId}` has no flow `{flowRef}` in its manifest.flows");

        var childFlow = LoadFlowCached(targetRoot, entry.Path, cache.Flows);
        return new ResolvedChildFlow
        {
            Label = $"{linkId}:{flowRef}",
            Graph = childFlow.Graph,
            Variables = childFlow.Variables,
            Manifest = childManifest,
            ProjectRoot = targetRoot,
        };
    }

    /// <summary>
    /// Fluxo de entrada do projeto vinculado: <c>entry_flow</c> quando difere de <c>flow.json</c>
    /// (id em <c>manifest.flows</c> ou caminho relativo); caso contrário, o próprio <c>flow.json</c>.
    /// </summary>
    static ResolvedChildFlow ResolveEntryFlow(LoadedProject loaded, string linkId, ChildFlowCache cache)
    {
        var entry = (loaded.Project.Manifest.EntryFlow ?? "").Trim();
        if (entry.Length == 0 || string.Equals(entry, ProjectLayout.FlowFile, StringComparison.OrdinalIgnoreCase))
        {
            return new ResolvedChildFlow
            {
                Label = $"{linkId}:entry",
                Graph = loaded.Project.Flow,
                Variables = new Dictionary<string, JsonElement>(loaded.Project.Variables, StringComparer.Ordinal),
                Manifest = loaded.Project.Manifest,
                ProjectRoot = loaded.Root,
            };
        }

        if (loaded.Project.Manifest.Flows is not null
            && loaded.Project.Manifest.Flows.TryGetValue(entry, out var byId))
        {
            var byIdFlow = LoadFlowCached(loaded.Root, byId.Path, cache.Flows);
            return new ResolvedChildFlow
            {
                Label = $"{linkId}:{entry}",
                Graph = byIdFlow.Graph,
                Variables = byIdFlow.Variables,
                Manifest = loaded.Project.Manifest,
                ProjectRoot = loaded.Root,
            };
        }

        var byPath = LoadFlowCached(loaded.Root, entry, cache.Flows);
        return new ResolvedChildFlow
        {
            Label = $"{linkId}:{entry}",
            Graph = byPath.Graph,
            Variables = byPath.Variables,
            Manifest = loaded.Project.Manifest,
            ProjectRoot = loaded.Root,
        };
    }

    /// <summary>Caminho do projeto vinculado: relativo à raiz do projeto chamador; nunca absoluto.</summary>
    public static string ResolveLinkedProjectPath(string projectRoot, string relative, string linkId)
    {
        if (string.IsNullOrWhiteSpace(relative))
            throw new InvalidOperationException($"linked project `{linkId}` has an empty path");
        if (Path.IsPathRooted(relative))
            throw new InvalidOperationException($"linked project `{linkId}` path must be relative to the caller project");

        var full = Path.GetFullPath(Path.Combine(projectRoot, relative.Trim()));
        if (!File.Exists(ProjectLayout.ManifestPath(full)) || !File.Exists(ProjectLayout.FlowPath(full)))
            throw new InvalidOperationException(
                $"linked project `{linkId}` not found: `{relative}` must contain {ProjectLayout.ManifestFile} and {ProjectLayout.FlowFile}");
        return full;
    }

    public static LoadedProject LoadProjectCached(string root, ChildFlowCache cache)
    {
        var key = Path.GetFullPath(root).ToLowerInvariant();
        if (cache.Projects.TryGetValue(key, out var cached))
            return cached;
        var loaded = new LoadedProject { Project = ProjectLoader.LoadProject(root), Root = Path.GetFullPath(root) };
        cache.Projects[key] = loaded;
        return loaded;
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
