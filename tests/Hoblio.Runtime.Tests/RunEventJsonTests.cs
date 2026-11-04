using System.Text.Json.Nodes;
using Hoblio.Runtime;

namespace Hoblio.Runtime.Tests;

public sealed class RunEventJsonTests
{
    [Fact]
    public void Action_executed_includes_module_and_indices()
    {
        var evt = new RunEvent.ActionExecuted(
            NodeId: "n1",
            Summary: "log: hi",
            Variables: null,
            FlowRef: "demo",
            LogIndex: 1,
            StepIndex: 2,
            Module: "System",
            ActionId: "LogMessage",
            Title: "My log",
            Status: "Succeeded");

        var json = RunEventJson.Serialize(evt);
        var node = JsonNode.Parse(json)!;
        Assert.Equal("action_executed", (string?)node["kind"]);
        Assert.Equal(1, (int?)node["log_index"]);
        Assert.Equal(2, (int?)node["step_index"]);
        Assert.Equal("System", (string?)node["module"]);
        Assert.Equal("LogMessage", (string?)node["action_id"]);
        Assert.Equal("demo", (string?)node["flow_ref"]);
        Assert.Equal("My log", (string?)node["title"]);
    }
}
