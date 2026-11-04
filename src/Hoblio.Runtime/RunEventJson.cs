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
        RunEvent.FlowStarted e => new JsonObject
        {
            ["kind"] = "flow_started",
            ["flow_ref"] = e.FlowRef,
            ["parent_node_id"] = e.ParentNodeId,
        }.ToJsonString(),
        RunEvent.FlowFinished e => new JsonObject
        {
            ["kind"] = "flow_finished",
            ["flow_ref"] = e.FlowRef,
            ["end_node_id"] = e.EndNodeId,
        }.ToJsonString(),
        _ => "{}",
    };

    static string SerializeActionExecuted(RunEvent.ActionExecuted e)
    {
        var obj = new JsonObject
        {
            ["kind"] = "action_executed",
            ["log_index"] = e.LogIndex,
            ["flow_ref"] = e.FlowRef,
            ["step_index"] = e.StepIndex,
            ["node_id"] = e.NodeId,
            ["action_id"] = e.ActionId,
            ["summary"] = e.Summary,
            ["status"] = e.Status,
            ["inside_error_handling"] = e.InsideErrorHandling,
        };
        if (!string.IsNullOrWhiteSpace(e.Module))
            obj["module"] = e.Module;
        if (!string.IsNullOrWhiteSpace(e.Title))
            obj["title"] = e.Title;
        if (!string.IsNullOrWhiteSpace(e.ErrorCode))
            obj["error_code"] = e.ErrorCode;
        WriteVariableNames(obj, "input_variable_names", e.InputVariableNames);
        WriteVariableNames(obj, "output_variable_names", e.OutputVariableNames);
        if (e.Variables is { Count: > 0 })
        {
            var vars = new JsonObject();
            foreach (var (k, v) in e.Variables)
                vars[k] = v;
            obj["variables"] = vars;
        }
        return obj.ToJsonString();
    }

    static void WriteVariableNames(JsonObject obj, string key, IReadOnlyList<string>? names)
    {
        if (names is not { Count: > 0 })
            return;
        if (names.Count == 1)
        {
            obj[key] = names[0];
            return;
        }
        var arr = new JsonArray();
        foreach (var n in names)
            arr.Add(n);
        obj[key] = arr;
    }
}
