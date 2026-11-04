using Hoblio.Runtime;
using Hoblio.Studio.ViewModels;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class RunLogLineTests
{
    [Fact]
    public void Action_executed_human_line_includes_module_and_action_id()
    {
        var line = RunLogLine.FromEvent(new RunEvent.ActionExecuted(
            "n1", "log: hi", null, "demo", 1, 1, "System", "LogMessage"));
        Assert.Contains("System.LogMessage", line.Human);
        Assert.Contains("n1", line.Human);
    }

    [Fact]
    public void Repeat_action_human_line_uses_action_id_only()
    {
        var line = RunLogLine.FromEvent(new RunEvent.ActionExecuted(
            "n1", "repeat action", null, "demo", 2, 1, null, "RepeatAction",
            InsideErrorHandling: true));
        Assert.Contains("RepeatAction", line.Human);
        Assert.DoesNotContain("System", line.Human);
    }
}
