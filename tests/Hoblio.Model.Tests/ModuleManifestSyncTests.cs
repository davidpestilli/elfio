using Hoblio.Model;
using Hoblio.Serde;
using Hoblio.TestSupport;

namespace Hoblio.Model.Tests;

public sealed class ModuleManifestSyncTests
{
    [Fact]
    public void Refresh_populates_module_references_from_graph()
    {
        var manifest = new FlowManifest { Name = "t", Version = "1" };
        var graph = new FlowGraph
        {
            Start = "s",
            Nodes =
            [
                new Node { Id = "s", Kind = NodeKind.Start },
                new Node
                {
                    Id = "b",
                    Kind = NodeKind.Action,
                    Action = new WebAutomationAction
                    {
                        Operation = WebAutomationActionTypes.Navigate,
                        Url = "https://example.com",
                    },
                },
                new Node { Id = "e", Kind = NodeKind.End },
            ],
            Edges = [],
        };

        ModuleManifestSync.Refresh(manifest, graph);

        Assert.Equal(ElfioEngineInfo.Version, manifest.EngineVersion);
        Assert.Equal(ElfioEngineInfo.Version, manifest.CreatedEngineVersion);
        Assert.NotNull(manifest.ModuleReferences);
        Assert.Contains(manifest.ModuleReferences!, r => r.Name == "WebAutomation");
    }

    [Fact]
    public void Refresh_does_not_stack_overflow_with_multiple_embedded_subflows()
    {
        var manifest = new FlowManifest { Name = "t", Version = "1" };
        var project = ProjectLoader.LoadProject(ExamplePaths.Resolve("login-eproc"));
        ModuleManifestSync.Refresh(manifest, project.Flow);
        Assert.NotNull(manifest.ModuleReferences);
        Assert.NotEmpty(manifest.ModuleReferences!);
    }
}
