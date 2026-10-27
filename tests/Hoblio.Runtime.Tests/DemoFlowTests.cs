using Hoblio.Runtime;
using Hoblio.Serde;
using Hoblio.TestSupport;

namespace Hoblio.Runtime.Tests;

public class DemoFlowTests
{
    [Fact]
    public void Flow_control_demo_runs()
    {
        var path = ExamplePaths.Resolve("flow-control-demo");
        var project = ProjectLoader.LoadProject(path);
        var outcome = FlowExecutor.RunProject(project, path);
        Assert.Equal("end", outcome.LastNodeId);
        var logs = outcome.Events.OfType<RunEvent.ActionExecuted>().Select(e => e.Summary).ToList();
        Assert.Contains(logs, s => s.Contains("branch then"));
        Assert.Contains(logs, s => s.Contains("item=beta"));
    }

    [Fact]
    public void Variables_loops_demo_runs()
    {
        var path = ExamplePaths.Resolve("variables-loops-demo");
        var project = ProjectLoader.LoadProject(path);
        var outcome = FlowExecutor.RunProject(project, path);
        Assert.Equal("end", outcome.LastNodeId);
        var logs = outcome.Events.OfType<RunEvent.ActionExecuted>().Select(e => e.Summary).ToList();
        Assert.Contains(logs, s => s.Contains("while body counter=0"));
        Assert.Contains(logs, s => s.Contains("done counter=4"));
        Assert.Contains(logs, s => s.Contains("repeat index=3"));
    }

    [Fact]
    public void Files_lists_demo_runs()
    {
        var path = ExamplePaths.Resolve("files-lists-demo");
        var project = ProjectLoader.LoadProject(path);
        var outcome = FlowExecutor.RunProject(project, path);
        Assert.Equal("end", outcome.LastNodeId);
        var reportPath = Path.Combine(path, "output", "report.txt");
        Assert.True(File.Exists(reportPath));
        var text = File.ReadAllText(reportPath);
        Assert.Contains("item=alpha", text);
        File.Delete(reportPath);
    }

    [Fact]
    public void Resilience_demo_runs()
    {
        var path = ExamplePaths.Resolve("resilience-demo");
        var project = ProjectLoader.LoadProject(path);
        var outcome = FlowExecutor.RunProject(project, path);
        Assert.Equal("end", outcome.LastNodeId);
        var logs = outcome.Events.OfType<RunEvent.ActionExecuted>().Select(e => e.Summary).ToList();
        Assert.Contains(logs, s => s.Contains("failed (continued)"));
        Assert.Contains(logs, s => s.Contains("continued after read failure"));
    }
}
