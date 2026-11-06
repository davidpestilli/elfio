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
        Assert.IsType<WebAutomationAction>(action);
        var back = JsonSerializer.Serialize(action, HoblioJson.Options);
        Assert.Contains("browser_get_text", back);
    }

    [Fact]
    public void Action_roundtrip_browser_upload()
    {
        var json = """{"type":"browser_upload","selector":"input[type=file]","path":"docs/peticao.pdf","timeout_ms":20000}""";
        var action = JsonSerializer.Deserialize<ActionKind>(json, HoblioJson.Options);
        var web = Assert.IsType<WebAutomationAction>(action);
        Assert.Equal(WebAutomationActionTypes.Upload, web.Operation);
        Assert.Equal("input[type=file]", web.Selector);
        Assert.Equal("docs/peticao.pdf", web.Path);
        Assert.Equal(20000ul, web.TimeoutMs);
        Assert.True(WebAutomationActionTypes.IsWebAutomationType(WebAutomationActionTypes.Upload));
        var back = JsonSerializer.Serialize(action, HoblioJson.Options);
        Assert.Contains("browser_upload", back);
        Assert.Contains("docs/peticao.pdf", back);
    }

    [Fact]
    public void Action_roundtrip_switch_page_and_dialog_action()
    {
        var json = """{"type":"browser_switch_page","url":"digitar_documento","timeout_ms":30000}""";
        var switchPage = Assert.IsType<WebAutomationAction>(JsonSerializer.Deserialize<ActionKind>(json, HoblioJson.Options));
        Assert.Equal(WebAutomationActionTypes.SwitchPage, switchPage.Operation);
        Assert.Equal("digitar_documento", switchPage.Url);

        var clickJson = """{"type":"browser_click","selector":"#btnSalvar","dialog_action":"accept"}""";
        var click = Assert.IsType<WebAutomationAction>(JsonSerializer.Deserialize<ActionKind>(clickJson, HoblioJson.Options));
        Assert.Equal(WebAutomationEnums.DialogAction.Accept, click.DialogAction);
        Assert.Contains("dialog_action", JsonSerializer.Serialize(click, HoblioJson.Options));
    }
}
