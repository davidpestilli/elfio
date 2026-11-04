using System.Runtime.CompilerServices;
using System.Windows;
using Hoblio.Model;
using Hoblio.Studio.FlowCanvas;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public StudioPendingConnectionViewModel PendingConnection { get; } = new();

    public RelayCommand<object?> ConnectionStartedCommand { get; private set; } = null!;
    public RelayCommand<object?> ConnectionCompletedCommand { get; private set; } = null!;
    public RelayCommand<object?> DisconnectConnectorCommand { get; private set; } = null!;
    public RelayCommand<object?> RemoveConnectionCommand { get; private set; } = null!;
    public RelayCommand DeleteDiagramSelectionCommand { get; private set; } = null!;

    void InitConnectionCommands()
    {
        ConnectionStartedCommand = new RelayCommand<object?>(OnConnectionStarted, _ => CanEditDiagram);
        ConnectionCompletedCommand = new RelayCommand<object?>(OnConnectionCompleted, _ => CanEditDiagram);
        DisconnectConnectorCommand = new RelayCommand<object?>(OnDisconnectConnector, _ => CanEditDiagram);
        RemoveConnectionCommand = new RelayCommand<object?>(OnRemoveConnection, _ => CanEditDiagram);
        DeleteDiagramSelectionCommand = new RelayCommand(DeleteDiagramSelection, () =>
            CanEditDiagram && (SelectedConnection is not null || CanDeleteAnySelectedNode()));
    }

    void OnConnectionStarted(object? source)
    {
        if (!CanEditDiagram)
            return;
        var connector = ResolveConnector(source);
        if (connector is null)
            return;
        PendingConnection.Source = connector;
        PendingConnection.Target = null;
        PendingConnection.IsVisible = true;
    }

    public void CompleteConnection(
        FlowConnectorViewModel source,
        FlowConnectorViewModel target,
        string? sourceHandle = null,
        string? targetHandle = null)
    {
        _canvasConnectSourceHandle = sourceHandle;
        _canvasConnectTargetHandle = targetHandle;
        OnConnectionCompleted(Tuple.Create<object?, object?>(source, target));
        _canvasConnectSourceHandle = null;
        _canvasConnectTargetHandle = null;
    }

    void OnConnectionCompleted(object? param)
    {
        if (!CanEditDiagram)
            return;
        PendingConnection.IsVisible = false;
        if (!TryResolveConnectionPair(param, out var src, out var tgt))
            return;
        if (!CanCreateConnection(src, tgt))
        {
            SetStatus("Conexão inválida — use saída → entrada em nós diferentes.");
            return;
        }

        var fromId = src.Node.Id;
        var toId = tgt.Node.Id;
        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();
        var port = src.Port;
        var replaced = edges.RemoveAll(e => e.From == fromId && e.ResolvedPort() == port);

        PushHistory();
        var normalizedSource = FlowCanvasHandles.NormalizeSourceHandle(_canvasConnectSourceHandle);
        var persistSource =
            normalizedSource is not null &&
            !string.Equals(normalizedSource, FlowCanvasHandles.MainSource, StringComparison.Ordinal) &&
            !(port != EdgePort.Main && string.Equals(normalizedSource, port.AsString(), StringComparison.Ordinal))
                ? normalizedSource
                : null;
        var persistTarget = string.IsNullOrEmpty(_canvasConnectTargetHandle) ||
                            string.Equals(_canvasConnectTargetHandle, "left", StringComparison.Ordinal)
            ? null
            : _canvasConnectTargetHandle;

        EdgeUi? ui = persistSource is not null || persistTarget is not null
            ? new EdgeUi { SourceHandle = persistSource, TargetHandle = persistTarget }
            : null;

        edges.Add(new Edge { From = fromId, To = toId, Port = port.AsString(), Ui = ui });
        CommitGraphSlice(nodes, edges);
        SetStatus(replaced > 0
            ? $"Substituída ligação de `{fromId}` ({port.AsString()}) → `{toId}`."
            : $"Ligado: {fromId} → {toId} ({port.AsString()}).");
    }

    static bool CanCreateConnection(FlowConnectorViewModel source, FlowConnectorViewModel target) =>
        !source.IsInput && target.IsInput &&
        !ReferenceEquals(source.Node, target.Node) &&
        source.Node.Kind is not NodeKind.End &&
        target.Node.Kind is not NodeKind.Start;

    static FlowConnectorViewModel? ResolveConnector(object? source) => source switch
    {
        FlowConnectorViewModel vm => vm,
        System.Windows.FrameworkElement { DataContext: FlowConnectorViewModel dc } => dc,
        _ => null,
    };

    static bool TryResolveConnectionPair(object? param, out FlowConnectorViewModel src, out FlowConnectorViewModel tgt)
    {
        src = null!;
        tgt = null!;
        object? a = null;
        object? b = null;
        switch (param)
        {
            case Tuple<object, object> tuple:
                a = tuple.Item1;
                b = tuple.Item2;
                break;
            case ITuple tuple when tuple.Length >= 2:
                a = tuple[0];
                b = tuple[1];
                break;
            default:
                return false;
        }

        var from = ResolveConnector(a);
        var to = ResolveConnector(b);
        if (from is null || to is null)
            return false;
        src = from;
        tgt = to;
        return true;
    }

    void OnDisconnectConnector(object? param)
    {
        var connector = ResolveConnector(param);
        if (connector is null || !CanEditDiagram)
            return;
        PushHistory();
        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();
        var removed = connector.IsInput
            ? edges.RemoveAll(e => e.To == connector.Node.Id)
            : edges.RemoveAll(e => e.From == connector.Node.Id);
        if (removed == 0)
            return;
        CommitGraphSlice(nodes, edges);
        SelectedConnection = null;
        SetStatus(connector.IsInput
            ? $"Entradas desligadas de `{connector.Node.Id}`."
            : $"Saídas desligadas de `{connector.Node.Id}`.");
    }

    void OnRemoveConnection(object? param)
    {
        if (param is FlowConnectionViewModel conn)
            RemoveConnectionModel(conn);
        else if (SelectedConnection is not null)
            RemoveConnectionModel(SelectedConnection);
    }

    void RemoveConnectionModel(FlowConnectionViewModel conn)
    {
        if (!CanEditDiagram)
            return;
        PushHistory();
        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();
        var port = conn.Port.AsString();
        var removed = edges.RemoveAll(e =>
            e.From == conn.Source.Id && e.To == conn.Target.Id &&
            string.Equals(e.ResolvedPort().AsString(), port, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
            return;
        CommitGraphSlice(nodes, edges);
        if (ReferenceEquals(SelectedConnection, conn))
            SelectedConnection = null;
        DeleteDiagramSelectionCommand.RaiseCanExecuteChanged();
        SetStatus($"Aresta removida: {conn.Source.Id} → {conn.Target.Id}.");
    }

    void DeleteDiagramSelection()
    {
        if (SelectedConnection is not null)
            RemoveConnectionModel(SelectedConnection);
        else if (CanDeleteAnySelectedNode())
            DeleteSelectedNodes();
    }

    bool CanDeleteAnySelectedNode() =>
        SelectedNodes.Any(n => n.Kind is not (NodeKind.Start or NodeKind.End));

    /// <summary>Grava posições do canvas no grafo ativo sem reconstruir a UI.</summary>
    void PersistCanvasToActiveGraph()
    {
        var uiMap = Nodes.ToDictionary(n => n.Id, n => n.Location);
        var (sliceNodes, sliceEdges, start) = ActiveGraphSlice();
        for (var i = 0; i < sliceNodes.Count; i++)
        {
            var node = sliceNodes[i];
            if (!uiMap.TryGetValue(node.Id, out var loc))
                continue;
            sliceNodes[i] = new Node
            {
                Id = node.Id,
                Kind = node.Kind,
                Label = node.Label,
                Action = node.Action,
                Control = node.Control,
                ErrorHandling = node.ErrorHandling,
                Ui = new NodeUi { X = loc.X, Y = loc.Y },
            };
        }
        WriteActiveGraphSlice(sliceNodes, sliceEdges);
        if (_activeSubflowId is null)
        {
            _project.Flow.Nodes = sliceNodes;
            _project.Flow.Edges = sliceEdges;
        }
    }

    void CommitGraphSlice(List<Node> nodes, List<Edge> edges)
    {
        WriteActiveGraphSlice(nodes, edges);
        if (_activeSubflowId is null)
        {
            _project.Flow.Nodes = nodes;
            _project.Flow.Edges = edges;
        }
        RebuildCanvas();
        MarkDirty();
        RefreshDiagramNodeIds();
        DeleteDiagramSelectionCommand.RaiseCanExecuteChanged();
    }

    void RefreshDiagramNodeIds()
    {
        DiagramNodeIds.Clear();
        foreach (var n in Nodes.OrderBy(n => n.Id, StringComparer.Ordinal))
            DiagramNodeIds.Add(n.Id);
        OnPropertyChanged(nameof(DiagramNodeIds));
    }

    void UpdateConnectorStates()
    {
        foreach (var node in Nodes)
        {
            if (node.Input is { } input)
                input.IsConnected = Connections.Any(c => c.Target.Id == node.Id);
            foreach (var output in node.OutputConnectors)
                output.IsConnected = Connections.Any(c =>
                    c.Source.Id == node.Id && c.Port == output.Port);
        }
    }

    Edge? FindEdgeModel(FlowConnectionViewModel conn)
    {
        var ( _, edges, _) = ActiveGraphSlice();
        var port = conn.Port.AsString();
        return edges.FirstOrDefault(e =>
            e.From == conn.Source.Id && e.To == conn.Target.Id &&
            string.Equals(e.ResolvedPort().AsString(), port, StringComparison.OrdinalIgnoreCase));
    }

    void PatchSelectedEdge(Action<Edge, int, List<Edge>> patch)
    {
        if (SelectedConnection is null || !CanEditDiagram)
            return;
        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();
        var edge = FindEdgeModel(SelectedConnection);
        if (edge is null)
            return;
        var idx = edges.IndexOf(edge);
        if (idx < 0)
            return;
        PushHistory();
        patch(edge, idx, edges);
        var updated = edges[idx];
        CommitGraphSlice(nodes, edges);
        ReselectConnection(updated.From, updated.To, updated.ResolvedPort());
    }

    void ReselectConnection(string from, string to, EdgePort port)
    {
        SelectedConnection = Connections.FirstOrDefault(c =>
            c.Source.Id == from && c.Target.Id == to && c.Port == port);
        OnPropertyChanged(nameof(SelectedEdgeTargetId));
        OnPropertyChanged(nameof(SelectedEdgeSourceId));
    }

    public string SelectedEdgeSourceId
    {
        get => SelectedConnection?.Source.Id ?? "";
        set
        {
            if (SelectedConnection is null || string.IsNullOrWhiteSpace(value) || value == SelectedConnection.Source.Id)
                return;
            var newFrom = value.Trim();
            if (Nodes.All(n => n.Id != newFrom))
                return;
            PatchSelectedEdge((edge, idx, edges) =>
            {
                edges[idx] = new Edge
                {
                    Id = edge.Id,
                    From = newFrom,
                    To = edge.To,
                    Port = edge.Port,
                    Label = edge.Label,
                    Ui = edge.Ui,
                };
            });
            OnPropertyChanged();
            SetStatus($"Origem atualizada → `{newFrom}`.");
        }
    }
}
