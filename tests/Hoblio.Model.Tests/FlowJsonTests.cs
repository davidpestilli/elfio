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
    public void Action_roundtrip_browser_get_text()
    {
        var json = """{"type":"browser_get_text","selector":"h1","variable":"title"}""";
        var action = JsonSerializer.Deserialize<ActionKind>(json, HoblioJson.Options);
        Assert.IsType<BrowserGetTextAction>(action);
        var back = JsonSerializer.Serialize(action, HoblioJson.Options);
        Assert.Contains("browser_get_text", back);
    }
}
