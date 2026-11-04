using System.Text.Json;
using System.Windows;
using Hoblio.Model;
using Hoblio.Studio.FlowCanvas;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    bool _canvasSelectionSync;
    int _canvasFitViewToken;
    string? _canvasConnectSourceHandle;
    string? _canvasConnectTargetHandle;

    public FlowCanvasHost? FlowCanvas { get; set; }

    public int CanvasFitViewToken => _canvasFitViewToken;

    public void NotifyFlowCanvasFull(bool requestFitView = false)
    {
        if (requestFitView)
            _canvasFitViewToken++;
        FlowCanvas?.SyncFromViewModel();
    }

    public void NotifyFlowCanvasNodePresentation(params FlowNodeViewModel[] nodes)
    {
        if (nodes.Length == 0)
            NotifyFlowCanvasFull();
        else
            FlowCanvas?.PatchNodePresentation(nodes);
    }

    public void CanvasApplyNodesMoved(JsonElement payload)
    {
        if (!CanEditDiagram || payload.ValueKind != JsonValueKind.Object)
            return;
        if (!payload.TryGetProperty("positions", out var positions) || positions.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in positions.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var idEl))
                continue;
            var id = idEl.GetString();
            if (string.IsNullOrEmpty(id))
                continue;
            if (!item.TryGetProperty("x", out var xEl) || !item.TryGetProperty("y", out var yEl))
                continue;
            var node = Nodes.FirstOrDefault(n => n.Id == id);
            if (node is null)
                continue;
            node.Location = new Point(xEl.GetDouble(), yEl.GetDouble());
        }

        MarkDirty();
        foreach (var conn in Connections)
            conn.NotifyGeometryChanged();
    }

    public void CanvasApplyConnect(JsonElement payload)
    {
        if (!CanEditDiagram || payload.ValueKind != JsonValueKind.Object)
            return;
        if (!payload.TryGetProperty("from", out var fromEl) || !payload.TryGetProperty("to", out var toEl))
            return;
        var fromId = fromEl.GetString();
        var toId = toEl.GetString();
        if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId))
            return;

        var src = Nodes.FirstOrDefault(n => n.Id == fromId);
        var tgt = Nodes.FirstOrDefault(n => n.Id == toId);
        if (src is null || tgt?.Input is null)
            return;

        var sourceHandle = payload.TryGetProperty("sourceHandle", out var sh)
            ? sh.GetString()
            : FlowCanvasHandles.MainSource;
        var targetHandle = payload.TryGetProperty("targetHandle", out var th)
            ? th.GetString()
            : "left";
        var port = FlowCanvasHandles.PortFromSourceHandle(src.Kind, sourceHandle);
        var srcOut = src.OutputConnectors.FirstOrDefault(c => c.Port == port)
                     ?? src.Output;
        if (srcOut is null)
            return;
        CompleteConnection(srcOut, tgt.Input, sourceHandle, targetHandle);
        NotifyFlowCanvasFull();
    }

    public void CanvasApplySelection(JsonElement payload)
    {
        if (_canvasSelectionSync || payload.ValueKind != JsonValueKind.Object)
            return;

        _canvasSelectionSync = true;
        try
        {
            var nodeIds = new List<string>();
            if (payload.TryGetProperty("nodeIds", out var nodesEl) && nodesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var n in nodesEl.EnumerateArray())
                {
                    var id = n.GetString();
                    if (!string.IsNullOrEmpty(id))
                        nodeIds.Add(id);
                }
            }

            string? edgeId = null;
            if (payload.TryGetProperty("edgeId", out var edgeEl) && edgeEl.ValueKind == JsonValueKind.String)
                edgeId = edgeEl.GetString();

            SelectedNodes.Clear();
            foreach (var id in nodeIds)
            {
                if (Nodes.FirstOrDefault(n => n.Id == id) is { } vm)
                    SelectedNodes.Add(vm);
            }

            OnPropertyChanged(nameof(SelectedNodeCount));
            OnPropertyChanged(nameof(HasMultiSelection));
            SyncPrimaryFromSelection();

            if (!string.IsNullOrEmpty(edgeId))
            {
                SelectedConnection = Connections.FirstOrDefault(c =>
                    FlowCanvasSerializer.EdgeId(c.Source.Id, c.Port, c.Target.Id) == edgeId);
            }
            else if (nodeIds.Count > 0)
                SelectedConnection = null;
        }
        finally
        {
            _canvasSelectionSync = false;
        }
    }

    public void CanvasApplyRemoveEdge(JsonElement payload)
    {
        if (!CanEditDiagram || payload.ValueKind != JsonValueKind.Object)
            return;
        if (!payload.TryGetProperty("from", out var fromEl) ||
            !payload.TryGetProperty("to", out var toEl) ||
            !payload.TryGetProperty("port", out var portEl))
            return;

        var from = fromEl.GetString();
        var to = toEl.GetString();
        var portStr = portEl.GetString();
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || string.IsNullOrEmpty(portStr))
            return;
        if (EdgePortExtensions.Parse(portStr) is not { } port)
            return;

        var conn = Connections.FirstOrDefault(c =>
            c.Source.Id == from && c.Target.Id == to && c.Port == port);
        if (conn is not null)
            RemoveConnectionModel(conn);
        NotifyFlowCanvasFull();
    }

    public void CanvasApplyWaypoints(JsonElement payload)
    {
        if (!CanEditDiagram || payload.ValueKind != JsonValueKind.Object)
            return;
        if (!payload.TryGetProperty("from", out var fromEl) ||
            !payload.TryGetProperty("to", out var toEl) ||
            !payload.TryGetProperty("port", out var portEl))
            return;

        var from = fromEl.GetString();
        var to = toEl.GetString();
        var portStr = portEl.GetString();
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || string.IsNullOrEmpty(portStr))
            return;
        if (EdgePortExtensions.Parse(portStr) is not { } port)
            return;

        var conn = Connections.FirstOrDefault(c =>
            c.Source.Id == from && c.Target.Id == to && c.Port == port);
        if (conn is null)
            return;

        var pushHistory = payload.TryGetProperty("history", out var h) && h.GetBoolean();
        if (pushHistory)
            PushHistory();

        conn.Waypoints.Clear();
        if (payload.TryGetProperty("waypoints", out var wps) && wps.ValueKind == JsonValueKind.Array)
        {
            foreach (var w in wps.EnumerateArray())
            {
                if (w.TryGetProperty("x", out var x) && w.TryGetProperty("y", out var y))
                    conn.Waypoints.Add(new Point(x.GetDouble(), y.GetDouble()));
            }
        }

        conn.NotifyGeometryChanged();
        MarkDirty();
        FlowCanvas?.PatchEdgeWaypoints(
            FlowCanvasSerializer.EdgeId(conn.Source.Id, conn.Port, conn.Target.Id),
            conn.Waypoints.Select(p => (p.X, p.Y)).ToList());
    }

    public void CanvasApplyConnectEnd(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            return;
        var ok = payload.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
        if (ok)
            return;
        var message = payload.TryGetProperty("message", out var msgEl)
            ? msgEl.GetString()
            : null;
        SetStatus(message ?? "Conexão inválida — use saída → entrada em nós diferentes.", isError: true);
    }

    public void CanvasApplyToolbar(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("action", out var actionEl))
            return;

        switch (actionEl.GetString())
        {
            case "undo":
                if (CanUndoDiagram)
                    Undo();
                break;
            case "redo":
                if (CanRedoDiagram)
                    Redo();
                break;
            case "clearFlow":
                if (CanEditDiagram)
                    ClearFlow();
                break;
            case "deleteSelection":
                if (DeleteDiagramSelectionCommand.CanExecute(null))
                    DeleteDiagramSelectionCommand.Execute(null);
                break;
            case "setEditing":
                if (payload.TryGetProperty("editing", out var editingEl) && !IsRunning)
                    IsEditing = editingEl.GetBoolean();
                break;
            case "clearEdgeSelection":
                SelectedConnection = null;
                break;
            case "setEdgeLabel":
                CanvasApplySetEdgeLabel(payload);
                break;
        }
    }

    void CanvasApplySetEdgeLabel(JsonElement payload)
    {
        if (!CanEditDiagram)
            return;
        if (!payload.TryGetProperty("edgeId", out var idEl) ||
            !payload.TryGetProperty("label", out var labelEl))
            return;
        var edgeId = idEl.GetString();
        if (string.IsNullOrEmpty(edgeId))
            return;
        var parts = edgeId.Split('|');
        if (parts.Length != 3)
            return;
        if (EdgePortExtensions.Parse(parts[1]) is not { } port)
            return;
        var from = parts[0];
        var to = parts[2];
        var label = labelEl.GetString() ?? "";
        var trimmed = string.IsNullOrWhiteSpace(label) ? null : label.Trim();

        PushHistory();
        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();
        var idx = edges.FindIndex(e =>
            e.From == from && e.To == to && e.ResolvedPort() == port);
        if (idx < 0)
            return;
        var edge = edges[idx];
        edges[idx] = new Edge
        {
            Id = edge.Id,
            From = edge.From,
            To = edge.To,
            Port = edge.Port,
            Label = trimmed,
            Ui = edge.Ui,
        };
        CommitGraphSlice(nodes, edges);
        ReselectConnection(from, to, port);
    }

    public void CanvasApplyShortcut(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("action", out var actionEl))
            return;

        switch (actionEl.GetString())
        {
            case "selectAll":
                if (CanEditDiagram)
                    SelectAllDiagramNodes();
                break;
            case "copy":
                if (CopySelectionCommand.CanExecute(null))
                    CopySelectionCommand.Execute(null);
                break;
            case "cut":
                if (CutSelectionCommand.CanExecute(null))
                    CutSelectionCommand.Execute(null);
                break;
            case "paste":
                if (PasteCommand.CanExecute(null))
                    PasteCommand.Execute(null);
                break;
            case "delete":
                if (DeleteDiagramSelectionCommand.CanExecute(null))
                    DeleteDiagramSelectionCommand.Execute(null);
                break;
        }
    }

    public void CanvasApplyReconnect(JsonElement payload)
    {
        if (!CanEditDiagram || payload.ValueKind != JsonValueKind.Object)
            return;
        if (!payload.TryGetProperty("from", out var fromEl) ||
            !payload.TryGetProperty("to", out var toEl) ||
            !payload.TryGetProperty("port", out var portEl) ||
            !payload.TryGetProperty("newFrom", out var newFromEl) ||
            !payload.TryGetProperty("newTo", out var newToEl))
            return;

        var from = fromEl.GetString();
        var to = toEl.GetString();
        var portStr = portEl.GetString();
        var newFrom = newFromEl.GetString();
        var newTo = newToEl.GetString();
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || string.IsNullOrEmpty(portStr) ||
            string.IsNullOrEmpty(newFrom) || string.IsNullOrEmpty(newTo))
            return;
        if (EdgePortExtensions.Parse(portStr) is not { } oldPort)
            return;

        var sourceHandle = payload.TryGetProperty("sourceHandle", out var sh) ? sh.GetString() : null;
        var targetHandle = payload.TryGetProperty("targetHandle", out var th) ? th.GetString() : null;

        var srcNode = Nodes.FirstOrDefault(n => n.Id == newFrom);
        if (srcNode is null)
            return;
        var port = FlowCanvasHandles.PortFromSourceHandle(srcNode.Kind, sourceHandle);

        PushHistory();
        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();
        var idx = edges.FindIndex(e =>
            e.From == from && e.To == to && e.ResolvedPort() == oldPort);
        if (idx < 0)
            return;

        var old = edges[idx];
        var waypoints = old.Ui?.Waypoints;
        edges.RemoveAt(idx);
        edges.RemoveAll(e => e.From == newFrom && e.ResolvedPort() == port);

        var normalizedSource = FlowCanvasHandles.NormalizeSourceHandle(sourceHandle);
        edges.Add(new Edge
        {
            Id = old.Id,
            From = newFrom,
            To = newTo,
            Port = port.AsString(),
            Label = old.Label,
            Ui = new EdgeUi
            {
                Waypoints = waypoints,
                SourceHandle = normalizedSource is FlowCanvasHandles.MainSource ? null : normalizedSource,
                TargetHandle = string.IsNullOrEmpty(targetHandle) || targetHandle == "left" ? null : targetHandle,
            },
        });

        CommitGraphSlice(nodes, edges);
        SetStatus($"Ligação reconectada: {newFrom} → {newTo} ({port.AsString()}).");
    }

    public void CanvasDeleteNodes(JsonElement payload)
    {
        if (!CanEditDiagram || payload.ValueKind != JsonValueKind.Object)
            return;
        if (!payload.TryGetProperty("ids", out var idsEl) || idsEl.ValueKind != JsonValueKind.Array)
            return;

        var ids = idsEl.EnumerateArray()
            .Select(e => e.GetString())
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet(StringComparer.Ordinal)!;

        if (ids.Count == 0)
            return;

        PushHistory();
        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();
        nodes.RemoveAll(n => ids.Contains(n.Id) && n.Kind is not NodeKind.Start);
        edges.RemoveAll(e => ids.Contains(e.From) || ids.Contains(e.To));
        CommitGraphSlice(nodes, edges);
        SelectSingleNode(null);
        SetStatus(ids.Count == 1 ? "Nó removido." : $"{ids.Count} nós removidos.");
    }
}
