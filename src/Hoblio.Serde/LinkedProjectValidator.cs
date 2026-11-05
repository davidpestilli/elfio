using Hoblio.Model;

namespace Hoblio.Serde;

/// <summary>
/// Validação de <c>manifest.linked_projects</c>: caminho do projeto vinculado existe, fluxo referenciado
/// existe, e a cadeia de vínculos não forma ciclo (A → B → A) nem excede a profundidade máxima de run_flow.
/// </summary>
public static class LinkedProjectValidator
{
    public static void Validate(FlowProject project, string projectRoot)
    {
        if (project.Manifest.LinkedProjects is not { Count: > 0 })
            return;

        var root = Path.GetFullPath(projectRoot);
        var cache = new ChildFlowCache();
        cache.Projects[root.ToLowerInvariant()] = new LoadedProject { Project = project, Root = root };

        ValidateGraphReferences(project.Manifest, project.Flow, root, cache);
        ValidateNoCycles(project.Manifest, root, [root], 0, cache);
    }

    static void ValidateGraphReferences(
        FlowManifest manifest,
        FlowGraph graph,
        string root,
        ChildFlowCache cache)
    {
        foreach (var node in graph.Nodes)
        {
            if (node.Action is not RunFlowAction { Project: { } projectId } action || string.IsNullOrWhiteSpace(projectId))
                continue;
            try
            {
                ChildFlowResolver.Resolve(root, manifest, graph, action, cache);
            }
            catch (InvalidOperationException ex)
            {
                throw new ValidationException(
                    ValidationErrorKind.InvalidLinkedProject,
                    $"node `{node.Id}` run_flow project `{projectId.Trim()}`: {ex.Message}");
            }
        }

        if (graph.Subflows is null)
            return;
        foreach (var bundle in graph.Subflows.Values)
            ValidateGraphReferences(manifest, bundle.AsGraph(null), root, cache);
    }

    static void ValidateNoCycles(
        FlowManifest manifest,
        string root,
        List<string> stack,
        int depth,
        ChildFlowCache cache)
    {
        if (manifest.LinkedProjects is null)
            return;

        foreach (var (id, link) in manifest.LinkedProjects.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(link.Path))
                throw new ValidationException(
                    ValidationErrorKind.InvalidLinkedProject,
                    $"manifest.linked_projects.`{id}` requires a `path`");

            var target = ResolveTarget(link.Path, id, root);
            if (stack.Any(p => string.Equals(p, target, StringComparison.OrdinalIgnoreCase)))
            {
                var chain = string.Join(" -> ", stack.Append(target).Select(Path.GetFileName));
                throw new ValidationException(
                    ValidationErrorKind.InvalidLinkedProject,
                    $"linked project cycle detected: {chain}");
            }

            if (depth + 1 >= ChildFlowResolver.MaxRunFlowDepth)
                throw new ValidationException(
                    ValidationErrorKind.InvalidLinkedProject,
                    $"linked project chain exceeds maximum depth {ChildFlowResolver.MaxRunFlowDepth}");

            var loaded = LoadProject(target, id, cache);
            stack.Add(target);
            try
            {
                ValidateGraphReferences(loaded.Project.Manifest, loaded.Project.Flow, target, cache);
                ValidateNoCycles(loaded.Project.Manifest, target, stack, depth + 1, cache);
            }
            finally
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }
    }

    static string ResolveTarget(string relativePath, string linkId, string root)
    {
        try
        {
            return ChildFlowResolver.ResolveLinkedProjectPath(root, relativePath, linkId);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ValidationErrorKind.InvalidLinkedProject, ex.Message);
        }
    }

    static LoadedProject LoadProject(string root, string linkId, ChildFlowCache cache)
    {
        try
        {
            return ChildFlowResolver.LoadProjectCached(root, cache);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Text.Json.JsonException)
        {
            throw new ValidationException(
                ValidationErrorKind.InvalidLinkedProject,
                $"manifest.linked_projects.`{linkId}`: {ex.Message}");
        }
    }
}
