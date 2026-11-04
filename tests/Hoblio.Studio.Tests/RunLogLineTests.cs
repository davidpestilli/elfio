using Hoblio.Runtime;
using Hoblio.Studio.ViewModels;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class RunLogLineTests
{
    [Fact]
    public void Action_executed_human_line_includes_module_and_action_id()
    {
        var fmt = new RunLogFormatter();
        var line = RunLogLine.FromEvent(new RunEvent.ActionExecuted(
            "n1", "log: hi", null, "demo", 1, 1, "System", "LogMessage"), fmt);
        Assert.Contains("System · LogMessage", line.Subtitle);
        Assert.Contains("n1", line.Subtitle);
        Assert.Equal("log: hi", line.Title);
        Assert.Equal("n1", line.NodeId);
        Assert.True(line.CanFocusNode);
    }

    [Fact]
    public void Repeat_action_human_line_uses_action_id_only()
    {
        var fmt = new RunLogFormatter();
        var line = RunLogLine.FromEvent(new RunEvent.ActionExecuted(
            "n1", "repeat action", null, "demo", 2, 1, null, "RepeatAction",
            InsideErrorHandling: true), fmt);
        Assert.Equal("RepeatAction", line.Subtitle!.Split('·')[0].Trim());
        Assert.DoesNotContain("System", line.Subtitle);
    }

    [Fact]
    public void Node_exit_is_hidden_from_timeline()
    {
        var fmt = new RunLogFormatter();
        fmt.Format(new RunEvent.NodeEnter("a"));
        var exit = fmt.Format(new RunEvent.NodeExit("a"));
        Assert.True(exit.IsHidden);
    }

    [Fact]
    public void Flow_events_increase_indent()
    {
        var fmt = new RunLogFormatter();
        fmt.Format(new RunEvent.RunStarted("demo"));
        var flow = fmt.Format(new RunEvent.FlowStarted("child", "parent"));
        Assert.Equal(0, flow.Indent);
        var action = fmt.Format(new RunEvent.ActionExecuted(
            "n", "set_variable: x=1", null, "child", 1, 1, "Variables", "Assign"));
        Assert.Equal(1, action.Indent);
    }
}
