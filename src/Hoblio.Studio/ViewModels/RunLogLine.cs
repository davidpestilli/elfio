using Hoblio.Runtime;

namespace Hoblio.Studio.ViewModels;

public sealed class RunLogLine
{
    public required string Human { get; init; }
    public required string Json { get; init; }

    public static RunLogLine FromEvent(RunEvent evt)
    {
        var json = RunEventJson.Serialize(evt);
        var human = evt switch
        {
            RunEvent.RunStarted e => $"run_started · project={e.Project}",
            RunEvent.NodeEnter e => $"node_enter · {e.NodeId}",
            RunEvent.NodeExit e => $"node_exit · {e.NodeId}",
            RunEvent.ActionExecuted e => $"action_executed · {e.NodeId} · {e.Summary}",
            RunEvent.RunFinished e => $"run_finished · last={e.LastNode}",
            RunEvent.FlowStarted e => $"flow_started · {e.FlowRef}",
            RunEvent.FlowFinished e => $"flow_finished · {e.FlowRef} → {e.EndNodeId}",
            _ => json,
        };
        return new RunLogLine { Human = human, Json = json };
    }
}
