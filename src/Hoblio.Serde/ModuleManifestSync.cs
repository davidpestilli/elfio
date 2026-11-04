using Hoblio.Model;

namespace Hoblio.Serde;

public static class ModuleManifestSync
{
    public static void Refresh(FlowManifest manifest, FlowGraph primaryGraph, string? projectDir = null)
    {
        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ActionCatalog.CollectModulesFromGraph(primaryGraph, modules);

        if (projectDir is not null && manifest.Flows is { Count: > 0 })
        {
            foreach (var entry in manifest.Flows.Values)
            {
                try
                {
                    var (graph, _) = ProjectLoader.LoadFlowDocument(projectDir, entry.Path);
                    ActionCatalog.CollectModulesFromGraph(graph, modules);
                }
                catch
                {
                    // auxiliary flow missing on disk — skip until saved
                }
            }
        }

        manifest.EngineVersion = ElfioEngineInfo.Version;
        if (string.IsNullOrWhiteSpace(manifest.CreatedEngineVersion))
            manifest.CreatedEngineVersion = ElfioEngineInfo.Version;

        manifest.ModuleReferences = modules
            .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
            .Select(name => new ModuleReference
            {
                Name = name,
                AssemblyName = ElfioEngineInfo.RuntimeAssembly,
                Version = ElfioEngineInfo.Version,
                Type = ElfioEngineInfo.BuiltInModuleType,
            })
            .ToList();
    }
}
