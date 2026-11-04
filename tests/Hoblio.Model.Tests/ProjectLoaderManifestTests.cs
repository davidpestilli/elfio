using System.Text.Json;
using Hoblio.Model;
using Hoblio.Model.Json;
using Hoblio.Serde;
using Hoblio.TestSupport;

namespace Hoblio.Model.Tests;

public sealed class ProjectLoaderManifestTests
{
    [Fact]
    public void UpdateManifestCapabilities_adds_module_references()
    {
        var source = ExamplePaths.Resolve("browser-demo-flow");
        var temp = Path.Combine(Path.GetTempPath(), "elfio-manifest-" + Guid.NewGuid().ToString("N"));
        try
        {
            CopyProject(source, temp);
            var manifestPath = ProjectLayout.ManifestPath(temp);
            var before = File.ReadAllText(manifestPath);
            Assert.DoesNotContain("module_references", before);

            var wrote = ProjectLoader.UpdateManifestCapabilities(temp);
            Assert.True(wrote);

            var manifest = JsonSerializer.Deserialize<FlowManifest>(File.ReadAllText(manifestPath), HoblioJson.Options);
            Assert.NotNull(manifest?.ModuleReferences);
            Assert.Contains(manifest.ModuleReferences, m => m.Name == "WebAutomation");
            Assert.Equal(ElfioEngineInfo.Version, manifest.EngineVersion);
        }
        finally
        {
            if (Directory.Exists(temp))
                Directory.Delete(temp, recursive: true);
        }
    }

    static void CopyProject(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var name = Path.GetFileName(file);
            if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                File.Copy(file, Path.Combine(destDir, name));
        }
    }
}
