using Hoblio.Runtime;
using Hoblio.Serde;
using Hoblio.TestSupport;

namespace Hoblio.Runtime.Tests;

public class DemoFlowTests
{
    [Fact]
    public void Demo_flow_action_executed_includes_catalog_ids()
    {
        var path = ExamplePaths.Resolve("demo-flow");
        var project = ProjectLoader.LoadProject(path);
        var outcome = FlowExecutor.RunProject(project, path);
        var first = outcome.Events.OfType<RunEvent.ActionExecuted>().First();
        Assert.Equal("System", first.Module);
        Assert.Equal("LogMessage", first.ActionId);
        Assert.True(first.LogIndex >= 1);
        Assert.Equal(project.Manifest.Name, first.FlowRef);
    }

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
    public void Variables_demo_flow_runs()
    {
        var path = ExamplePaths.Resolve("variables-demo-flow");
        var project = ProjectLoader.LoadProject(path);
        var outcome = FlowExecutor.RunProject(project, path);
        Assert.Equal("end", outcome.LastNodeId);

        var exportPath = Path.Combine(path, "output", "export.csv");
        var summaryPath = Path.Combine(path, "output", "summary.txt");
        Assert.True(File.Exists(exportPath));
        Assert.True(File.Exists(summaryPath));
        var export = File.ReadAllText(exportPath);
        Assert.Contains("Carol", export);
        Assert.Contains(";", export);
        var summary = File.ReadAllText(summaryPath);
        Assert.Contains("shared", summary);
        Assert.Contains("\"demo\":true", summary);
        Assert.Contains("A001", summary);
        Assert.Contains("team=Eng", summary);

        var variableActions = outcome.Events.OfType<RunEvent.ActionExecuted>()
            .Where(e => e.Module == "Variables")
            .ToList();
        Assert.True(variableActions.Count >= 28);
        Assert.Contains(variableActions, e => e.ActionId == "CreateNewDatatable");
        Assert.Contains(variableActions, e => e.ActionId == "JoinDataTable");
        Assert.Contains(variableActions, e => e.ActionId == "DecreaseVariable");

        var doneLog = outcome.Events.OfType<RunEvent.ActionExecuted>()
            .Select(e => e.Summary)
            .FirstOrDefault(s => s.Contains("variables-demo complete"));
        Assert.NotNull(doneLog);
        Assert.Contains("counter=0", doneLog);

        File.Delete(exportPath);
        File.Delete(summaryPath);
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
    public void Flow_compose_demo_runs()
    {
        var path = ExamplePaths.Resolve("flow-compose-demo");
        var project = ProjectLoader.LoadProject(path);
        var outcome = FlowExecutor.RunProject(project, path);
        Assert.Equal("end", outcome.LastNodeId);
        var logs = outcome.Events.OfType<RunEvent.ActionExecuted>().Select(e => e.Summary).ToList();
        Assert.Contains(logs, s => s.Contains("run_flow") && s.Contains("greet"));
        Assert.Contains(logs, s => s.Contains("From child:"));
        Assert.Contains(logs, s => s.Contains("Done flag:"));
    }

    [Fact]
    public void Flow_compose_subflow_runs()
    {
        var path = ExamplePaths.Resolve("flow-compose-subflow");
        var project = ProjectLoader.LoadProject(path);
        var outcome = FlowExecutor.RunProject(project, path);
        Assert.Equal("end", outcome.LastNodeId);
        var logs = outcome.Events.OfType<RunEvent.ActionExecuted>().Select(e => e.Summary).ToList();
        Assert.Contains(logs, s => s.Contains("run_flow") && s.Contains("greet_inline"));
        Assert.Contains(logs, s => s.Contains("Message:"));
    }

    [Fact]
    public void Resilience_demo_emits_repeat_action_on_retry()
    {
        var path = ExamplePaths.Resolve("resilience-demo");
        var project = ProjectLoader.LoadProject(path);
        var outcome = FlowExecutor.RunProject(project, path);
        var repeats = outcome.Events.OfType<RunEvent.ActionExecuted>()
            .Where(e => e.ActionId == "RepeatAction")
            .ToList();
        Assert.NotEmpty(repeats);
        Assert.All(repeats, e => Assert.True(e.InsideErrorHandling));
        Assert.All(repeats, e => Assert.Null(e.Module));
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
