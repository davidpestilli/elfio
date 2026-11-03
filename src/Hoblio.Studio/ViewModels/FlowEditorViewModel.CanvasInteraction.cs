using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    bool _syncingCanvasSelection;

    /// <summary>Fonte ligada a <c>NodifyEditor.SelectedItems</c> (itens do diagrama, não containers).</summary>
    public ObservableCollection<object> DiagramSelectedItems { get; } = [];

    public ObservableCollection<FlowNodeViewModel> SelectedNodes { get; } = [];

    public int SelectedNodeCount => SelectedNodes.Count;

    public bool HasMultiSelection => SelectedNodes.Count > 1;

    public RelayCommand ItemsDragCompletedCommand { get; private set; } = null!;

    void InitCanvasInteraction()
    {
        ItemsDragCompletedCommand = new RelayCommand(OnItemsDragCompleted, () => CanEditDiagram);
        SelectedNodes.CollectionChanged += OnSelectedNodesCollectionChanged;
    }

    void OnSelectedNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_syncingCanvasSelection)
            return;
        OnPropertyChanged(nameof(SelectedNodeCount));
        OnPropertyChanged(nameof(HasMultiSelection));
        SyncPrimaryFromSelection();
    }

    public void ApplyCanvasSelection(IEnumerable<object> items)
    {
        if (_syncingCanvasSelection)
            return;
        _syncingCanvasSelection = true;
        try
        {
            SelectedNodes.Clear();
            foreach (var item in items)
            {
                if (ResolveDiagramNode(item) is { } node)
                    SelectedNodes.Add(node);
            }
        }
        finally
        {
            _syncingCanvasSelection = false;
        }
        OnPropertyChanged(nameof(SelectedNodeCount));
        OnPropertyChanged(nameof(HasMultiSelection));
        SyncPrimaryFromSelection();
    }

    void SyncPrimaryFromSelection()
    {
        var first = SelectedNodes.FirstOrDefault();
        if (ReferenceEquals(SelectedNode, first) && _selectedNodeId == first?.Id)
            return;
        _selectedNodeVm = first;
        _selectedNodeId = first?.Id;
        OnPropertyChanged(nameof(SelectedNode));
        OnPropertyChanged(nameof(SelectedNodeId));
        OnPropertyChanged(nameof(HasSelectedNode));
        OnPropertyChanged(nameof(NoSelectedNode));
        SelectedConnection = null;
        RefreshAllInspector();
        RefreshInspectorVisibility();
        DeleteNodeCommand?.RaiseCanExecuteChanged();
        DeleteDiagramSelectionCommand?.RaiseCanExecuteChanged();
        RaiseClipboardCommands();
    }

    public void SelectAllDiagramNodes()
    {
        if (!CanEditDiagram || Nodes.Count == 0)
            return;
        _syncingCanvasSelection = true;
        try
        {
            SelectedNodes.Clear();
            foreach (var node in Nodes)
                SelectedNodes.Add(node);
        }
        finally
        {
            _syncingCanvasSelection = false;
        }
        OnPropertyChanged(nameof(SelectedNodeCount));
        OnPropertyChanged(nameof(HasMultiSelection));
        SyncPrimaryFromSelection();
        PushDiagramSelection(SelectedNodes.ToList());
        SetStatus($"{SelectedNodes.Count} nós selecionados — arraste um deles para mover o fluxo.");
    }

    public void SelectSingleNode(FlowNodeViewModel? node)
    {
        _syncingCanvasSelection = true;
        try
        {
            SelectedNodes.Clear();
            if (node is not null)
                SelectedNodes.Add(node);
        }
        finally
        {
            _syncingCanvasSelection = false;
        }
        OnPropertyChanged(nameof(SelectedNodeCount));
        OnPropertyChanged(nameof(HasMultiSelection));
        SyncPrimaryFromSelection();
        PushDiagramSelection(node is null ? [] : [node]);
    }

    public void ToggleNodeInSelection(FlowNodeViewModel node)
    {
        if (!CanEditDiagram)
            return;
        _syncingCanvasSelection = true;
        try
        {
            var idx = SelectedNodes.IndexOf(node);
            if (idx >= 0)
                SelectedNodes.RemoveAt(idx);
            else
                SelectedNodes.Add(node);
        }
        finally
        {
            _syncingCanvasSelection = false;
        }
        OnPropertyChanged(nameof(SelectedNodeCount));
        OnPropertyChanged(nameof(HasMultiSelection));
        SyncPrimaryFromSelection();
        PushDiagramSelection(SelectedNodes.ToList());
    }

    void PushDiagramSelection(IReadOnlyList<FlowNodeViewModel> nodes)
    {
        _syncingCanvasSelection = true;
        try
        {
            DiagramSelectedItems.Clear();
            foreach (var node in nodes)
                DiagramSelectedItems.Add(node);
        }
        finally
        {
            _syncingCanvasSelection = false;
        }
    }

    static FlowNodeViewModel? ResolveDiagramNode(object item) => item switch
    {
        FlowNodeViewModel vm => vm,
        _ => null,
    };

    public void ClearDiagramSelection() => SelectSingleNode(null);

    void OnItemsDragCompleted()
    {
        if (!CanEditDiagram)
            return;
        MarkDirty();
        ScheduleConnectionAnchorSync();
    }

    void DeleteSelectedNodes()
    {
        var victims = SelectedNodes
            .Where(n => n.Kind is not (NodeKind.Start or NodeKind.End))
            .ToList();
        if (victims.Count == 0)
            return;
        PushHistory();
        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();
        var ids = victims.Select(v => v.Id).ToHashSet(StringComparer.Ordinal);
        nodes.RemoveAll(n => ids.Contains(n.Id));
        edges.RemoveAll(e => ids.Contains(e.From) || ids.Contains(e.To));
        CommitGraphSlice(nodes, edges);
        SelectSingleNode(null);
        SetStatus(victims.Count == 1
            ? $"Nó `{victims[0].Id}` removido."
            : $"{victims.Count} nós removidos.");
    }
}
