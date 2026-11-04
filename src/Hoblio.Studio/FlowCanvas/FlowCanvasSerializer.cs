using System.Text.Json;
using System.Text.Json.Serialization;
using Hoblio.Model;
using Hoblio.Studio.ViewModels;

namespace Hoblio.Studio.FlowCanvas;

public static class FlowCanvasSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string EdgeId(string from, EdgePort port, string to) =>
        $"{from}|{port.AsString()}|{to}";

    public static object BuildSetGraph(FlowEditorViewModel vm)
    {
        var nodes = vm.Nodes.Select(n => new
        {
            id = n.Id,
            kind = KindToCanvas(n.Kind),
            label = n.Label,
            summary = n.Summary,
            x = n.Location.X,
            y = n.Location.Y,
            isActive = n.IsActive,
        }).ToArray();

        var edges = vm.Connections.Select(c =>
        {
            var sourceHandle = FlowCanvasHandles.SourceHandleFor(c.Source.Kind, c.Port, c.SourceHandle);
            var targetHandle = FlowCanvasHandles.DefaultTargetHandle(c.TargetHandle);
            return new
            {
                id = EdgeId(c.Source.Id, c.Port, c.Target.Id),
                from = c.Source.Id,
                to = c.Target.Id,
                port = c.Port.AsString(),
                sourceHandle,
                targetHandle,
                label = FlowCanvasHandles.CanvasEdgeLabel(c.Port, c.Label),
                waypoints = c.Waypoints.Select(w => new { x = w.X, y = w.Y }).ToArray(),
            };
        }).ToArray();

        string? selectedEdgeId = null;
        object? selectedEdge = null;
        if (vm.SelectedConnection is { } sel)
        {
            selectedEdgeId = EdgeId(sel.Source.Id, sel.Port, sel.Target.Id);
            selectedEdge = new
            {
                id = selectedEdgeId,
                sourceId = sel.Source.Id,
                targetId = sel.Target.Id,
                sourceLabel = sel.Source.Label,
                targetLabel = sel.Target.Label,
                label = FlowCanvasHandles.CanvasEdgeLabel(sel.Port, sel.Label) ?? "",
            };
        }

        return new
        {
            type = "setGraph",
            payload = new
            {
                nodes,
                edges,
                canEdit = vm.CanEditDiagram,
                canUndo = vm.CanUndoDiagram,
                canRedo = vm.CanRedoDiagram,
                selectedNodeIds = vm.SelectedNodes.Select(n => n.Id).ToArray(),
                selectedEdgeId,
                selectedEdge,
                fitViewToken = vm.CanvasFitViewToken,
            },
        };
    }

    static string KindToCanvas(NodeKind kind) => kind switch
    {
        NodeKind.ForEach => "for_each",
        NodeKind.NextLoop => "next_loop",
        NodeKind.ExitLoop => "exit_loop",
        _ => kind.ToString().ToLowerInvariant(),
    };
}
