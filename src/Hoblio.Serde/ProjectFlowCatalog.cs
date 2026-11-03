using Hoblio.Model;

namespace Hoblio.Serde;

public sealed record ProjectFlowEntry(string Id, string Path, string? Description);

public static class ProjectFlowCatalog
{
    public const string DefaultEntryFlow = ProjectLayout.FlowFile;

    public static IReadOnlyList<ProjectFlowEntry> ListFlows(FlowManifest manifest)
    {
        var entry = manifest.EntryFlow ?? DefaultEntryFlow;
        var list = new List<ProjectFlowEntry>
        {
            new("entry", entry, "Fluxo principal (orquestrador)"),
        };
        if (manifest.Flows is null)
            return list;
        foreach (var (id, meta) in manifest.Flows.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (string.Equals(meta.Path, entry, StringComparison.OrdinalIgnoreCase))
                continue;
            list.Add(new ProjectFlowEntry(id, meta.Path, meta.Description));
        }
        return list;
    }
}
