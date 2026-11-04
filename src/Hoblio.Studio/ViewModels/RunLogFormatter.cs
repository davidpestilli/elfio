using Hoblio.Runtime;

namespace Hoblio.Studio.ViewModels;

public sealed class RunLogFormatter
{
    int _flowDepth;

    public void Reset() => _flowDepth = 0;

    public RunLogLine Format(RunEvent evt)
    {
        var json = RunEventJson.Serialize(evt);
        return evt switch
        {
            RunEvent.RunStarted e => Line(
                RunLogLineKind.Run,
                0,
                $"Execução iniciada — projeto «{e.Project}»",
                null,
                false,
                false,
                HumanExport($"▶ RUN  project={e.Project}"),
                json),
            RunEvent.RunFinished e => Line(
                RunLogLineKind.Run,
                0,
                $"Execução concluída — último nó «{e.LastNode}»",
                null,
                false,
                false,
                HumanExport($"■ RUN  last={e.LastNode}"),
                json,
                e.LastNode),
            RunEvent.FlowStarted e => EnterFlow(e),
            RunEvent.FlowFinished e => ExitFlow(e),
            RunEvent.NodeEnter e => Line(
                RunLogLineKind.Node,
                _flowDepth,
                $"Entrada · {e.NodeId}",
                null,
                false,
                isMuted: true,
                HumanExport($"{IndentSpaces()}→ {e.NodeId}"),
                json,
                e.NodeId),
            RunEvent.NodeExit e => Skip(RunEventJson.Serialize(e)),
            RunEvent.ActionExecuted e => FormatAction(e, json),
            _ => Line(
                RunLogLineKind.System,
                _flowDepth,
                json,
                null,
                false,
                false,
                json,
                json),
        };
    }

    RunLogLine EnterFlow(RunEvent.FlowStarted e)
    {
        var indent = _flowDepth;
        _flowDepth++;
        var parent = string.IsNullOrWhiteSpace(e.ParentNodeId) ? null : $"chamado em «{e.ParentNodeId}»";
        return Line(
            RunLogLineKind.Flow,
            indent,
            $"Subfluxo «{e.FlowRef}»",
            parent,
            false,
            false,
            HumanExport($"{IndentSpaces()}📂 FLOW {e.FlowRef}"),
            RunEventJson.Serialize(e),
            e.ParentNodeId);
    }

    RunLogLine ExitFlow(RunEvent.FlowFinished e)
    {
        _flowDepth = Math.Max(0, _flowDepth - 1);
        return Line(
            RunLogLineKind.Flow,
            _flowDepth,
            $"Subfluxo concluído «{e.FlowRef}» → «{e.EndNodeId}»",
            null,
            false,
            false,
            HumanExport($"{IndentSpaces()}✓ FLOW {e.FlowRef} → {e.EndNodeId}"),
            RunEventJson.Serialize(e),
            e.EndNodeId);
    }

    RunLogLine FormatAction(RunEvent.ActionExecuted e, string json)
    {
        var failed = e.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)
                     || e.Summary.Contains("failed", StringComparison.OrdinalIgnoreCase);
        var moduleLine = string.IsNullOrWhiteSpace(e.Module)
            ? e.ActionId
            : $"{e.Module} · {e.ActionId}";
        return Line(
            RunLogLineKind.Action,
            _flowDepth,
            e.Summary,
            $"{moduleLine} · nó «{e.NodeId}»",
            failed,
            false,
            HumanExport($"{IndentSpaces()}▸ [{moduleLine}] {e.NodeId}: {e.Summary}"),
            json,
            e.NodeId);
    }

    static RunLogLine Skip(string json) =>
        new()
        {
            Kind = RunLogLineKind.Node,
            Indent = 0,
            Title = "",
            IsMuted = true,
            IsHidden = true,
            Human = "",
            Json = json,
        };

    RunLogLine Line(
        RunLogLineKind kind,
        int indent,
        string title,
        string? subtitle,
        bool isError,
        bool isMuted,
        string human,
        string json,
        string? nodeId = null) =>
        new()
        {
            Kind = kind,
            Indent = indent,
            Title = title,
            Subtitle = subtitle,
            IsError = isError,
            IsMuted = isMuted,
            Human = human,
            Json = json,
            NodeId = nodeId,
        };

    string IndentSpaces() => new string(' ', _flowDepth * 2);

    static string HumanExport(string line) => line;
}

public enum RunLogLineKind
{
    Run,
    Flow,
    Node,
    Action,
    System,
}
