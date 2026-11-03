using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using Hoblio.Model;
using Hoblio.Serde;

namespace Hoblio.Studio.ViewModels;

public sealed class EditorTabViewModel
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public bool IsSubflow => Key.StartsWith("subflow:", StringComparison.Ordinal);
    public bool IsFile => Key.StartsWith("file:", StringComparison.Ordinal);
}

public sealed partial class FlowEditorViewModel
{
    string? _activeSubflowId;
    int _paletteSpawnIndex;
    string _entryFlowPath = ProjectLayout.FlowFile;
    string _activeFlowPath = ProjectLayout.FlowFile;
    readonly Dictionary<string, FlowFileSnapshot> _flowFiles = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<EditorTabViewModel> EditorTabs { get; } = [];
    public ObservableCollection<string> ManifestFlowIds { get; } = [];

    public string? ActiveSubflowId
    {
        get => _activeSubflowId;
        private set
        {
            _activeSubflowId = value;
            _activeSubflowRenameDraft = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActiveEditorTitle));
            OnPropertyChanged(nameof(ActiveSubflowRenameDraft));
            RebuildEditorTabs();
        }
    }

    public string ActiveFlowPath
    {
        get => _activeFlowPath;
        private set
        {
            _activeFlowPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActiveEditorTitle));
            OnPropertyChanged(nameof(ActiveSubflowDefinedHint));
            RebuildEditorTabs();
        }
    }

    public string ActiveEditorTitle
    {
        get
        {
            var file = _activeFlowPath;
            if (_activeSubflowId is not null)
                return $"{file} · subfluxo {_activeSubflowId}";
            return file;
        }
    }

    public RelayCommand<string?> SwitchEditorCommand { get; private set; } = null!;
    public RelayCommand CreateSubflowCommand { get; private set; } = null!;
    public RelayCommand<FlowConnectionViewModel?> SelectConnectionCommand { get; private set; } = null!;

    void InitSubflowCommands()
    {
        SwitchEditorCommand = new RelayCommand<string?>(SwitchEditor);
        CreateSubflowCommand = new RelayCommand(CreateSubflow, () => CanEditDiagram && _activeSubflowId is null);
        SelectConnectionCommand = new RelayCommand<FlowConnectionViewModel?>(SelectConnection);
    }

    void InitFlowFileState(FlowProject project, string? projectDir)
    {
        _entryFlowPath = project.Manifest.EntryFlow ?? ProjectLayout.FlowFile;
        _activeFlowPath = _entryFlowPath;
        _activeSubflowId = null;
        _flowFiles.Clear();
        _flowFiles[_entryFlowPath] = new FlowFileSnapshot
        {
            Graph = project.Flow,
            Variables = new Dictionary<string, JsonElement>(project.Variables, StringComparer.Ordinal),
        };
        ManifestFlowIds.Clear();
        if (project.Manifest.Flows is not null)
        {
            foreach (var id in project.Manifest.Flows.Keys.OrderBy(k => k, StringComparer.Ordinal))
                ManifestFlowIds.Add(id);
        }
        RebuildEditorTabs();
        RefreshRunFlowTargets();
        _ = projectDir;
    }

    void EnsureFlowLoaded(string relativePath)
    {
        if (_flowFiles.ContainsKey(relativePath))
            return;
        if (_projectDir is null)
            throw new InvalidOperationException("Abra o projeto em uma pasta antes de editar fluxos filhos.");
        var (graph, variables) = ProjectLoader.LoadFlowDocument(_projectDir, relativePath);
        _flowFiles[relativePath] = new FlowFileSnapshot { Graph = graph, Variables = variables };
        if (string.Equals(relativePath, _entryFlowPath, StringComparison.OrdinalIgnoreCase))
        {
            _project.Flow = graph;
            _project.Variables = variables;
            _variablesJson = JsonSerializer.Serialize(variables, new JsonSerializerOptions { WriteIndented = true });
            OnPropertyChanged(nameof(VariablesJson));
        }
    }

    FlowFileSnapshot CurrentFileSnapshot()
    {
        EnsureFlowLoaded(_activeFlowPath);
        return _flowFiles[_activeFlowPath];
    }

    FlowGraph CurrentFileGraph() => CurrentFileSnapshot().Graph;

    void RebuildEditorTabs()
    {
        EditorTabs.Clear();
        if (_flowFiles.Count == 0)
        {
            EditorTabs.Add(new EditorTabViewModel { Key = "file:" + _entryFlowPath, Title = "Principal" });
            return;
        }
        foreach (var entry in ProjectFlowCatalog.ListFlows(_project.Manifest))
        {
            var title = entry.Id == "entry" ? "Principal" : entry.Id;
            EditorTabs.Add(new EditorTabViewModel
            {
                Key = "file:" + entry.Path,
                Title = title,
                Subtitle = entry.Path,
            });
        }
        var graph = CurrentFileGraph();
        if (graph.Subflows is not null && _activeSubflowId is null)
        {
            foreach (var id in graph.Subflows.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                EditorTabs.Add(new EditorTabViewModel
                {
                    Key = "subflow:" + id,
                    Title = id,
                    Subtitle = "subfluxo embutido",
                });
            }
        }
        else if (_activeSubflowId is not null)
        {
            EditorTabs.Add(new EditorTabViewModel
            {
                Key = "subflow:" + _activeSubflowId,
                Title = _activeSubflowId,
                Subtitle = "subfluxo embutido",
            });
        }
    }

    void SwitchEditor(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return;
        SyncUiToProject();
        if (key.StartsWith("file:", StringComparison.Ordinal))
        {
            ActiveSubflowId = null;
            ActiveFlowPath = key["file:".Length..];
            EnsureFlowLoaded(_activeFlowPath);
            if (string.Equals(_activeFlowPath, _entryFlowPath, StringComparison.OrdinalIgnoreCase))
            {
                var entrySnap = _flowFiles[_activeFlowPath];
                _project.Flow = entrySnap.Graph;
                _project.Variables = entrySnap.Variables;
            }
        }
        else if (key.StartsWith("subflow:", StringComparison.Ordinal))
        {
            ActiveSubflowId = key["subflow:".Length..];
        }
        var snap = CurrentFileSnapshot();
        _variablesJson = JsonSerializer.Serialize(snap.Variables, new JsonSerializerOptions { WriteIndented = true });
        OnPropertyChanged(nameof(VariablesJson));
        OnPropertyChanged(nameof(VariablesAdvancedJson));
        ReloadProjectVariablesUi();
        _paletteSpawnIndex = 0;
        RebuildCanvas();
        RefreshInspectorVisibility();
    }

    void CreateSubflow()
    {
        if (!CanEditDiagram || _activeSubflowId is not null)
            return;
        PushHistory();
        var graph = CurrentFileGraph();
        graph.Subflows ??= new Dictionary<string, SubflowBundle>(StringComparer.Ordinal);
        var id = NewSubflowIdUnique(graph.Subflows);
        graph.Subflows[id] = DefaultSubflowBundle();
        ActiveSubflowId = id;
        RebuildEditorTabs();
        RebuildCanvas();
        RefreshRunFlowTargets();
        MarkDirty();
        SetStatus($"Subfluxo \"{id}\" criado.");
    }

    static string NewSubflowIdUnique(IReadOnlyDictionary<string, SubflowBundle> subflows)
    {
        for (var n = 1; n < 100; n++)
        {
            var id = n == 1 ? "subflow_1" : $"subflow_{n}";
            if (!subflows.ContainsKey(id))
                return id;
        }
        return "subflow_" + Guid.NewGuid().ToString("N")[..8];
    }

    static SubflowBundle DefaultSubflowBundle() => new()
    {
        Start = "sf_start",
        Nodes =
        [
            new Node { Id = "sf_start", Kind = NodeKind.Start, Label = "Start", Ui = new NodeUi { X = 80, Y = 80 } },
            new Node { Id = "sf_end", Kind = NodeKind.End, Label = "End", Ui = new NodeUi { X = 320, Y = 80 } },
        ],
        Edges = [new Edge { From = "sf_start", To = "sf_end", Port = "main" }],
    };

    (List<Node> nodes, List<Edge> edges, string start) ActiveGraphSlice()
    {
        var graph = CurrentFileGraph();
        if (_activeSubflowId is null)
            return (graph.Nodes, graph.Edges, graph.Start);
        if (graph.Subflows is null || !graph.Subflows.TryGetValue(_activeSubflowId, out var bundle))
            throw new InvalidOperationException($"subflow `{_activeSubflowId}` missing");
        return (bundle.Nodes, bundle.Edges, bundle.Start);
    }

    void WriteActiveGraphSlice(List<Node> nodes, List<Edge> edges)
    {
        var graph = CurrentFileGraph();
        if (_activeSubflowId is null)
        {
            graph.Nodes = nodes;
            graph.Edges = edges;
            if (string.Equals(_activeFlowPath, _entryFlowPath, StringComparison.OrdinalIgnoreCase))
            {
                _project.Flow = graph;
            }
            return;
        }
        if (graph.Subflows is null || !graph.Subflows.TryGetValue(_activeSubflowId, out var bundle))
            return;
        bundle.Nodes = nodes;
        bundle.Edges = edges;
    }

    void SaveAllFlowFiles()
    {
        if (_projectDir is null)
            return;
        SyncUiToProject();
        foreach (var (path, snap) in _flowFiles)
        {
            if (string.Equals(path, _entryFlowPath, StringComparison.OrdinalIgnoreCase))
                continue;
            ProjectLoader.SaveFlowDocument(_projectDir, path, snap.Graph, snap.Variables);
        }
    }

    void SelectConnection(FlowConnectionViewModel? connection)
    {
        SelectedConnection = connection;
        foreach (var c in Connections)
            c.IsSelected = ReferenceEquals(c, connection);
    }

    public void MoveWaypoint(FlowConnectionViewModel connection, int index, Point point)
    {
        if (!CanEditDiagram || index < 0 || index >= connection.Waypoints.Count)
            return;
        connection.Waypoints[index] = point;
        connection.NotifyGeometryChanged();
        MarkDirty();
    }

    public void RemoveWaypoint(FlowConnectionViewModel connection, int index)
    {
        if (!CanEditDiagram || index < 0 || index >= connection.Waypoints.Count)
            return;
        PushHistory();
        connection.Waypoints.RemoveAt(index);
        connection.NotifyGeometryChanged();
        MarkDirty();
    }

    public void InsertWaypoint(FlowConnectionViewModel connection, int segmentIndex)
    {
        if (!CanEditDiagram)
            return;
        var points = connection.GetPolylinePoints();
        if (segmentIndex < 0 || segmentIndex >= points.Count - 1)
            return;
        PushHistory();
        var a = points[segmentIndex];
        var b = points[segmentIndex + 1];
        connection.Waypoints.Insert(segmentIndex, new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2));
        connection.NotifyGeometryChanged();
        MarkDirty();
    }
}
