using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hoblio.Runtime;

public static class RunEventJson
{
    public static string Serialize(RunEvent evt) => evt switch
    {
        RunEvent.RunStarted e => new JsonObject
        {
            ["kind"] = "run_started",
            ["project"] = e.Project,
        }.ToJsonString(),
        RunEvent.NodeEnter e => new JsonObject
        {
            ["kind"] = "node_enter",
            ["node_id"] = e.NodeId,
        }.ToJsonString(),
        RunEvent.NodeExit e => new JsonObject
        {
            ["kind"] = "node_exit",
            ["node_id"] = e.NodeId,
        }.ToJsonString(),
        RunEvent.ActionExecuted e => SerializeActionExecuted(e),
        RunEvent.RunFinished e => new JsonObject
        {
            ["kind"] = "run_finished",
            ["last_node"] = e.LastNode,
        }.ToJsonString(),
        _ => "{}",
    };

    static string SerializeActionExecuted(RunEvent.ActionExecuted e)
    {
        var obj = new JsonObject
        {
            ["kind"] = "action_executed",
            ["node_id"] = e.NodeId,
            ["summary"] = e.Summary,
        };
        if (e.Variables is { Count: > 0 })
        {
            var vars = new JsonObject();
            foreach (var (k, v) in e.Variables)
                vars[k] = v;
            obj["variables"] = vars;
        }
        return obj.ToJsonString();
    }
}
