using System.Text.Json;
using Hoblio.Model;
using Hoblio.Model.Json;
using Hoblio.Serde;
using Hoblio.TestSupport;

namespace Hoblio.Model.Tests;

public class FlowJsonTests
{
    [Fact]
    public void Loads_flow_control_demo_from_rust_repo()
    {
        var dir = ExamplePaths.Resolve("flow-control-demo");
        var project = ProjectLoader.LoadProject(dir);
        GraphValidator.ValidateProject(project);
        Assert.Equal("flow-control-demo", project.Manifest.Name);
    }

    [Fact]
    public void Login_eproc_refactored_switch_graph_validates()
    {
        var dir = ExamplePaths.Resolve("login-eproc");
        var project = ProjectLoader.LoadProject(dir);
        GraphValidator.ValidateProject(project);
        Assert.Contains(project.Flow.Nodes, n => n.Kind == NodeKind.Switch);
        Assert.Contains(project.Flow.Subflows!.Keys, k => k == "detect_screen");
    }

    [Fact]
    public void Login_eproc_save_roundtrip()
    {
        var dir = ExamplePaths.Resolve("login-eproc");
        var project = ProjectLoader.LoadProject(dir);
        var temp = Path.Combine(Path.GetTempPath(), "hoblio-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temp);
            ProjectLoader.SaveProject(temp, project);
            var reloaded = ProjectLoader.LoadProject(temp);
            GraphValidator.ValidateProject(reloaded);
            Assert.Contains(reloaded.Flow.Nodes, n => n.Kind == NodeKind.Switch);
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public void Action_roundtrip_browser_get_text()
    {
        var json = """{"type":"browser_get_text","selector":"h1","variable":"title"}""";
        var action = JsonSerializer.Deserialize<ActionKind>(json, HoblioJson.Options);
        Assert.IsType<BrowserGetTextAction>(action);
        var back = JsonSerializer.Serialize(action, HoblioJson.Options);
        Assert.Contains("browser_get_text", back);
    }
}
