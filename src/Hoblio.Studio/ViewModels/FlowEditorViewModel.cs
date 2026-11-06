using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Hoblio.Model;
using Hoblio.Runtime;
using Hoblio.Serde;
using Hoblio.Studio;
using Hoblio.Studio.FlowCanvas;

namespace Hoblio.Studio.ViewModels;

public sealed class FlowNodeViewModel : INotifyPropertyChanged
{
    public required string Id { get; init; }
    public required NodeKind Kind { get; init; }

    string _label = "";
    public string Label
    {
        get => _label;
        set { _label = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label))); }
    }

    string _summary = "";
    public string Summary
    {
        get => _summary;
        set { _summary = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary))); }
    }

    Point _location;
    public Point Location
    {
        get => _location;
        set { _location = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location))); }
    }

    bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive))); }
    }

    public bool HasInput => Kind is not NodeKind.Start;
    public bool HasOutput => Kind is not NodeKind.End;

    public FlowConnectorViewModel? Input { get; private set; }
    public FlowConnectorViewModel? Output { get; private set; }
    public ObservableCollection<FlowConnectorViewModel> InputConnectors { get; } = [];
    public ObservableCollection<FlowConnectorViewModel> OutputConnectors { get; } = [];

    public void AttachConnectors()
    {
        InputConnectors.Clear();
        OutputConnectors.Clear();
        if (HasInput)
        {
            Input = new FlowConnectorViewModel { Node = this, IsInput = true };
            InputConnectors.Add(Input);
        }
        else
            Input = null;

        if (HasOutput)
        {
            foreach (var port in NodeOutputPorts.ForKind(Kind))
            {
                var connector = new FlowConnectorViewModel { Node = this, IsInput = false, Port = port };
                OutputConnectors.Add(connector);
            }
            Output = OutputConnectors.FirstOrDefault(c => c.Port == EdgePort.Main)
                     ?? OutputConnectors.FirstOrDefault();
        }
        else
            Output = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed partial class FlowEditorViewModel : INotifyPropertyChanged
{
    readonly Stack<string> _undoStack = new();
    readonly Stack<string> _redoStack = new();
    const int HistoryLimit = 40;
    readonly object _runGate = new();
    CancellationTokenSource? _runCts;
    string _savedDigest = "";

    public ObservableCollection<FlowNodeViewModel> Nodes { get; } = [];
    public ObservableCollection<FlowConnectionViewModel> Connections { get; } = [];
    public ObservableCollection<string> DiagramNodeIds { get; } = [];
    public ObservableCollection<RunLogLine> LogLines { get; } = [];
    readonly RunLogFormatter _runLogFormatter = new();
    DispatcherTimer? _logNoticeTimer;

    string? _logToolbarNotice;
    public string? LogToolbarNotice
    {
        get => _logToolbarNotice;
        private set
        {
            _logToolbarNotice = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowLogToolbarNotice));
        }
    }

    public bool ShowLogToolbarNotice => !string.IsNullOrWhiteSpace(_logToolbarNotice);
    public ObservableCollection<PaletteGroupViewModel> PaletteGroups { get; } = BuildPaletteGroups();
    public ObservableCollection<PaletteGroupViewModel> FilteredPaletteGroups { get; } = [];

    static ObservableCollection<PaletteGroupViewModel> BuildPaletteGroups()
    {
        var groups = new ObservableCollection<PaletteGroupViewModel>();
        foreach (var g in ActionPaletteCatalog.Groups)
        {
            groups.Add(new PaletteGroupViewModel
            {
                Name = g.Name,
                StartsExpanded = g.DefaultExpanded,
                Items = new ObservableCollection<PaletteEntryViewModel>(
                    g.Items.Select(PaletteEntryViewModel.FromCatalogEntry)),
                Sections = new ObservableCollection<PaletteSectionViewModel>(
                    g.Sections.Select(s => new PaletteSectionViewModel
                    {
                        Name = s.Name,
                        StartsExpanded = s.DefaultExpanded,
                        Items = new ObservableCollection<PaletteEntryViewModel>(
                            s.Items.Select(PaletteEntryViewModel.FromCatalogEntry)),
                    })),
            });
        }
        return groups;
    }

    string _paletteFilter = "";
    public string PaletteFilter
    {
        get => _paletteFilter;
        set
        {
            _paletteFilter = value ?? "";
            OnPropertyChanged();
            RebuildFilteredPalette();
        }
    }

    public bool ShowEmptyFilteredPalette =>
        _paletteFilter.Trim().Length > 0 && FilteredPaletteGroups.Count == 0;

    public bool ShowPaletteReadOnlyHint => !CanEditDiagram;

    public string PaletteReadOnlyHint =>
        IsRunning
            ? "Execução em andamento — a paleta está pausada."
            : "Ative Edição na barra do diagrama para adicionar ações.";

    public RelayCommand ClearPaletteFilterCommand { get; }

    bool _isEditing = true;
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (_isEditing == value)
                return;
            _isEditing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsViewMode));
            OnPropertyChanged(nameof(CanEditDiagram));
            OnPropertyChanged(nameof(InspectorReadOnly));
            RefreshInspectorVisibility();
            RaiseClipboardCommands();
            NotifyFlowCanvasFull();
        }
    }

    public bool IsViewMode
    {
        get => !IsEditing;
        set
        {
            if (value)
                IsEditing = false;
            else if (!IsEditing)
                IsEditing = true;
        }
    }
    public bool CanEditDiagram => IsEditing && !IsRunning;

    public bool CanUndoDiagram => CanEditDiagram && _undoStack.Count > 0;

    public bool CanRedoDiagram => CanEditDiagram && _redoStack.Count > 0;

    FlowNodeViewModel? _selectedNodeVm;
    public FlowNodeViewModel? SelectedNode
    {
        get => _selectedNodeVm;
        set
        {
            if (ReferenceEquals(_selectedNodeVm, value))
                return;
            _selectedNodeVm = value;
            _selectedNodeId = value?.Id;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedNodeId));
            RefreshAllInspector();
            RefreshInspectorVisibility();
            DeleteNodeCommand?.RaiseCanExecuteChanged();
        }
    }

    public bool HasSelectedNode => SelectedNode is not null;
    public bool NoSelectedNode => SelectedNode is null;
    public bool ShowWelcomeHint => Nodes.Count <= 3 && string.IsNullOrEmpty(_projectDir);

    bool _statusIsError;
    public bool StatusIsError
    {
        get => _statusIsError;
        private set { _statusIsError = value; OnPropertyChanged(); }
    }

    FlowProject _project = DefaultFlow.Create();
    string? _projectDir;

    string? _selectedNodeId;
    FlowConnectionViewModel? _selectedConnection;
    string _variablesJson = "{}";
    string _status = "Pronto";
    bool _isRunning;
    bool _showLogJson;

    public string? ProjectPath
    {
        get => _projectDir;
        private set { _projectDir = value; OnPropertyChanged(); OnPropertyChanged(nameof(DocumentTitle)); }
    }

    public string DocumentTitle => ProjectDisplayName() + (IsDirty ? " *" : "");

    static bool IsPlaceholderProjectName(string? name) =>
        string.IsNullOrWhiteSpace(name)
        || string.Equals(name.Trim(), "untitled", StringComparison.OrdinalIgnoreCase);

    string ProjectDisplayName()
    {
        var name = _project.Manifest.Name?.Trim();
        if (!IsPlaceholderProjectName(name))
            return name!;
        if (!string.IsNullOrWhiteSpace(_projectDir))
        {
            var folder = Path.GetFileName(_projectDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrWhiteSpace(folder))
                return folder;
        }
        return "Sem título";
    }

    public bool IsDirty { get; private set; }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            _isRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRun));
            OnPropertyChanged(nameof(CanStop));
            OnPropertyChanged(nameof(CanEditDiagram));
            RunCommand.RaiseCanExecuteChanged();
            StopRunCommand.RaiseCanExecuteChanged();
            RefreshInspectorVisibility();
            NotifyFlowCanvasFull();
        }
    }

    public bool CanRun => !IsRunning && _project is not null;
    public bool CanStop => IsRunning;

    public bool ShowLogJson
    {
        get => _showLogJson;
        set { _showLogJson = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        private set { _status = value; OnPropertyChanged(); }
    }

    public string? SelectedNodeId
    {
        get => _selectedNodeId;
        set
        {
            if (_selectedNodeId == value)
                return;
            _selectedNodeId = value;
            _selectedNodeVm = value is null ? null : Nodes.FirstOrDefault(n => n.Id == value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedNode));
            OnPropertyChanged(nameof(HasSelectedNode));
            OnPropertyChanged(nameof(NoSelectedNode));
            RefreshAllInspector();
            RefreshInspectorVisibility();
            DeleteNodeCommand?.RaiseCanExecuteChanged();
        }
    }

    public FlowConnectionViewModel? SelectedConnection
    {
        get => _selectedConnection;
        set
        {
            _selectedConnection = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedEdgePort));
            RemoveEdgeCommand?.RaiseCanExecuteChanged();
            AddWaypointCommand?.RaiseCanExecuteChanged();
            DeleteDiagramSelectionCommand?.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(SelectedEdgeTargetId));
            OnPropertyChanged(nameof(ShowConnectionProperties));
            RefreshPropertiesPanelContext();
            OnPropertyChanged(nameof(SelectedConnectionFromId));
            OnPropertyChanged(nameof(SelectedEdgeSourceId));
            foreach (var c in Connections)
                c.IsSelected = ReferenceEquals(c, value);
            OnPropertyChanged(nameof(SelectedEdgeCanvasLabel));
            if (!_canvasSelectionSync)
                NotifyFlowCanvasFull();
        }
    }

    public bool ShowConnectionProperties => SelectedConnection is not null;

    public string? SelectedEdgeCanvasLabel =>
        SelectedConnection is { } c
            ? $"{c.Source.Id} → {c.Target.Id} ({c.Port.AsString()})"
            : null;

    public string SelectedConnectionFromId => SelectedConnection?.Source.Id ?? "";

    public string SelectedEdgePort
    {
        get => SelectedConnection?.Port.AsString() ?? "main";
        set
        {
            if (SelectedConnection is null)
                return;
            if (EdgePortExtensions.Parse(value) is not { } port)
                return;
            if (SelectedConnection.Port == port)
                return;
            PatchSelectedEdge((edge, idx, edges) =>
            {
                SelectedConnection.Port = port;
                edges[idx] = new Edge
                {
                    Id = edge.Id,
                    From = edge.From,
                    To = edge.To,
                    Port = port.AsString(),
                    Label = edge.Label,
                    Ui = edge.Ui,
                };
            });
            OnPropertyChanged();
        }
    }

    public string VariablesJson
    {
        get => _variablesJson;
        set
        {
            _variablesJson = value;
            OnPropertyChanged();
            try
            {
                using var doc = JsonDocument.Parse(value);
                var vars = doc.RootElement.EnumerateObject()
                    .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal);
                _project.Variables = vars;
                if (_flowFiles.ContainsKey(_activeFlowPath))
                    CurrentFileSnapshot().Variables = new Dictionary<string, JsonElement>(vars, StringComparer.Ordinal);
                ReloadProjectVariablesUi();
                MarkDirty();
            }
            catch
            {
                SetStatus("JSON de variáveis inválido", isError: true);
            }
        }
    }

    public string NodeLabelEdit
    {
        get => GetSelectedNode()?.Label ?? "";
        set
        {
            var node = GetSelectedNode();
            if (node is null || node.Kind == NodeKind.Start)
                return;
            node.Label = value;
            var vm = Nodes.FirstOrDefault(n => n.Id == node.Id);
            if (vm is not null)
            {
                vm.Label = value;
                NotifyFlowCanvasNodePresentation(vm);
            }
            MarkDirty();
            OnPropertyChanged();
        }
    }

    public string LogMessageEdit
    {
        get => GetSelectedNode()?.Action is LogAction log ? log.Message : "";
        set
        {
            var node = GetSelectedNode();
            if (node?.Kind != NodeKind.Action)
                return;
            node.Action = new LogAction { Message = value };
            CommitActionEdit(node);
            OnPropertyChanged();
        }
    }

    public RelayCommand<string> AddPaletteEntryCommand { get; }
    public RelayCommand AddWaypointCommand { get; }
    public RelayCommand ClearLogCommand { get; }
    public RelayCommand CopyLogCommand { get; }

    public RelayCommand OpenProjectCommand { get; }
    public RelayCommand NewProjectCommand { get; }
    public RelayCommand SaveProjectCommand { get; }
    public RelayCommand ValidateCommand { get; }
    public RelayCommand RunCommand { get; }
    public RelayCommand StopRunCommand { get; }
    public RelayCommand AddNodeCommand { get; }
    public RelayCommand ClearFlowCommand { get; }
    public RelayCommand UndoCommand { get; }
    public RelayCommand RedoCommand { get; }

    public FlowEditorViewModel()
    {
        OpenProjectCommand = new RelayCommand(OpenProject);
        NewProjectCommand = new RelayCommand(NewProject);
        SaveProjectCommand = new RelayCommand(SaveProject, () => _project is not null);
        ValidateCommand = new RelayCommand(Validate, () => _project is not null);
        RunCommand = new RelayCommand(() => _ = RunAsync(), () => CanRun);
        StopRunCommand = new RelayCommand(StopRun, () => CanStop);
        AddNodeCommand = new RelayCommand(() => AddNodeFromPalette("action:log"));
        AddPaletteEntryCommand = new RelayCommand<string>(key => AddNodeFromPalette(key ?? "action:log"), _ => CanEditDiagram);
        AddWaypointCommand = new RelayCommand(AddWaypoint, () => CanEditDiagram && SelectedConnection is not null);
        CopyLogCommand = new RelayCommand(CopyLogToClipboard, () => LogLines.Count > 0);
        ClearLogCommand = new RelayCommand(ClearRunLog, () => LogLines.Count > 0);
        ClearFlowCommand = new RelayCommand(ClearFlow, () => CanEditDiagram);
        UndoCommand = new RelayCommand(Undo, () => CanEditDiagram && _undoStack.Count > 0);
        RedoCommand = new RelayCommand(Redo, () => CanEditDiagram && _redoStack.Count > 0);
        ClearPaletteFilterCommand = new RelayCommand(
            () => PaletteFilter = "",
            () => _paletteFilter.Trim().Length > 0);
        InitRunLogCommands();
        InitInspectorCommands();
        InitSubflowCommands();
        InitConnectionCommands();
        InitCanvasInteraction();
        InitRunFlowCommands();
        InitProjectVariablesCommands();
        InitPropertiesPanelCommands();
        InitClipboardCommands();
        InitTemplateVariableCommands();
        InitCdpBrowserCommands();
        RebuildFilteredPalette();
        LoadProject(_project, null, resetUndo: true);
    }

    void RebuildFilteredPalette()
    {
        FilteredPaletteGroups.Clear();
        var q = _paletteFilter.Trim();
        foreach (var group in PaletteGroups)
        {
            if (group.HasSections)
            {
                var sections = new List<PaletteSectionViewModel>();
                foreach (var section in group.Sections)
                {
                    var items = section.Items
                        .Where(i => PaletteSearch.Matches(i.SearchIndex, q))
                        .ToList();
                    if (items.Count == 0)
                        continue;
                    sections.Add(new PaletteSectionViewModel
                    {
                        Name = section.Name,
                        StartsExpanded = q.Length > 0 || section.StartsExpanded,
                        Items = new ObservableCollection<PaletteEntryViewModel>(items),
                    });
                }

                if (sections.Count == 0)
                    continue;
                FilteredPaletteGroups.Add(new PaletteGroupViewModel
                {
                    Name = group.Name,
                    StartsExpanded = q.Length > 0 || group.StartsExpanded,
                    Sections = new ObservableCollection<PaletteSectionViewModel>(sections),
                });
                continue;
            }

            var flatItems = group.Items
                .Where(i => PaletteSearch.Matches(i.SearchIndex, q))
                .ToList();
            if (flatItems.Count == 0)
                continue;
            FilteredPaletteGroups.Add(new PaletteGroupViewModel
            {
                Name = group.Name,
                StartsExpanded = q.Length > 0 || group.StartsExpanded,
                Items = new ObservableCollection<PaletteEntryViewModel>(flatItems),
            });
        }

        OnPropertyChanged(nameof(ShowEmptyFilteredPalette));
        ClearPaletteFilterCommand.RaiseCanExecuteChanged();
    }

    void SetStatus(string message, bool isError = false)
    {
        Status = message;
        StatusIsError = isError;
    }

    void RefreshInspectorVisibility()
    {
        OnPropertyChanged(nameof(HasSelectedNode));
        OnPropertyChanged(nameof(NoSelectedNode));
        OnPropertyChanged(nameof(ShowWelcomeHint));
        OnPropertyChanged(nameof(ShowActionSection));
        OnPropertyChanged(nameof(ShowIfSection));
        OnPropertyChanged(nameof(ShowSwitchSection));
        OnPropertyChanged(nameof(ShowCaseSection));
        OnPropertyChanged(nameof(ShowCaseDefaultSection));
        OnPropertyChanged(nameof(ShowForEachSection));
        OnPropertyChanged(nameof(ShowWhileSection));
        OnPropertyChanged(nameof(ShowRepeatSection));
        OnPropertyChanged(nameof(ShowActionErrorSection));
        OnPropertyChanged(nameof(ShowGenericFields));
        OnPropertyChanged(nameof(ShowGenericField2));
        OnPropertyChanged(nameof(ShowGenericField3));
        OnPropertyChanged(nameof(ShowRunFlowSection));
        OnPropertyChanged(nameof(ShowRunFlowOpenSubflow));
        OnPropertyChanged(nameof(InspectorReadOnly));
        OpenSelectedRunFlowSubflowCommand?.RaiseCanExecuteChanged();
        AddPaletteEntryCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(ShowPaletteReadOnlyHint));
        OnPropertyChanged(nameof(PaletteReadOnlyHint));
        CreateSubflowCommand?.RaiseCanExecuteChanged();
        ClearFlowCommand.RaiseCanExecuteChanged();
        UndoCommand.RaiseCanExecuteChanged();
        RedoCommand.RaiseCanExecuteChanged();
        RefreshPropertiesPanelVisibility();
    }

    public bool ConfirmDiscardChanges()
    {
        if (!IsDirty)
            return true;
        return MessageBox.Show(
            "Há alterações não salvas. Descartar?",
            "Hoblio Studio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    void NewProject()
    {
        if (!ConfirmDiscardChanges())
            return;
        LoadProject(DefaultFlow.Create(), null, resetUndo: true);
    }

    void OpenProject()
    {
        if (!ConfirmDiscardChanges())
            return;
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Abrir manifest.json do projeto",
            Filter = "manifest.json|manifest.json",
        };
        if (dlg.ShowDialog() != true)
            return;
        var dir = Path.GetDirectoryName(dlg.FileName)!;
        LoadProject(ProjectLoader.LoadProject(dir), dir, resetUndo: true);
    }

    void LoadProject(FlowProject project, string? dir, bool resetUndo)
    {
        if (!resetUndo)
            PushHistory();
        _project = project;
        ProjectPath = dir;
        _variablesJson = JsonSerializer.Serialize(project.Variables, new JsonSerializerOptions { WriteIndented = true });
        InitFlowFileState(project, dir);
        PaletteSpawn.Reset();
        RebuildCanvas(requestCanvasFitView: true);
        _savedDigest = ProjectDigest();
        IsDirty = false;
        OnPropertyChanged(nameof(DocumentTitle));
        if (resetUndo)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            RaiseHistoryCommands();
        }
        SetStatus("Projeto carregado");
        OnPropertyChanged(nameof(ShowWelcomeHint));
    }

    void RebuildCanvas(bool requestCanvasFitView = false)
    {
        Nodes.Clear();
        Connections.Clear();
        var vmById = new Dictionary<string, FlowNodeViewModel>();
        var (sliceNodes, sliceEdges, _) = ActiveGraphSlice();
        foreach (var node in sliceNodes)
        {
            var vm = new FlowNodeViewModel
            {
                Id = node.Id,
                Kind = node.Kind,
                Label = node.Label ?? node.Id,
                Summary = Summarize(node),
                Location = new Point(node.Ui?.X ?? 0, node.Ui?.Y ?? 0),
            };
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(FlowNodeViewModel.Location))
                    MarkDirty();
            };
            vm.AttachConnectors();
            vmById[node.Id] = vm;
            Nodes.Add(vm);
        }
        SetActiveHighlight();
        OnPropertyChanged(nameof(ShowWelcomeHint));
        foreach (var edge in sliceEdges)
        {
            if (!vmById.TryGetValue(edge.From, out var src) || !vmById.TryGetValue(edge.To, out var tgt))
                continue;
            var conn = new FlowConnectionViewModel
            {
                Source = src,
                Target = tgt,
                Port = edge.ResolvedPort(),
                Label = edge.Label,
                SourceHandle = FlowCanvasHandles.SourceHandleFor(
                    src.Kind, edge.ResolvedPort(), edge.Ui?.SourceHandle),
                TargetHandle = FlowCanvasHandles.DefaultTargetHandle(edge.Ui?.TargetHandle),
            };
            if (edge.Ui?.Waypoints is { Count: > 0 } wps)
            {
                foreach (var w in wps)
                    conn.Waypoints.Add(new Point(w.X, w.Y));
            }
            conn.BindAnchorUpdates();
            Connections.Add(conn);
        }
        UpdateConnectorStates();
        RefreshDiagramNodeIds();
        ScheduleConnectionAnchorSync();
        ClearDiagramSelection();
        RefreshRunFlowTargets();
        ReloadProjectVariablesUi();
        RebuildTemplateVariableNames();
        NotifyFlowCanvasFull(requestCanvasFitView);
    }

    void ScheduleConnectionAnchorSync()
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            foreach (var node in Nodes)
            {
                node.Input?.RefreshAnchor();
                node.Output?.RefreshAnchor();
            }
            foreach (var conn in Connections)
                conn.NotifyGeometryChanged();
        }, DispatcherPriority.Loaded);
    }

    void SaveProject()
    {
        if (_projectDir is null)
        {
            MessageBox.Show("Use Abrir em uma pasta de projeto existente ou salve manualmente via Serde.", "Salvar");
            return;
        }
        try
        {
            CommitProjectVariablesFromUi();
            SyncUiToProject();
            SaveAllFlowFiles();
            ProjectLoader.SaveProject(_projectDir, _project);
            _savedDigest = ProjectDigest();
            IsDirty = false;
            OnPropertyChanged(nameof(DocumentTitle));
            SetStatus("Salvo");
        }
        catch (Exception ex)
        {
            SetStatus("Falha ao salvar", isError: true);
            MessageBox.Show(
                ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace,
                "Erro ao salvar",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    void SyncUiToProject()
    {
        var uiMap = new Dictionary<string, Point>(StringComparer.Ordinal);
        foreach (var n in Nodes)
            uiMap[n.Id] = n.Location;

        var (sliceNodes, _, start) = ActiveGraphSlice();
        var updatedNodes = sliceNodes.Select(node => new Node
        {
            Id = node.Id,
            Kind = node.Kind,
            Label = node.Label,
            Action = node.Action,
            Control = node.Control,
            ErrorHandling = node.ErrorHandling,
            Ui = uiMap.TryGetValue(node.Id, out var loc) ? new NodeUi { X = loc.X, Y = loc.Y } : node.Ui,
        }).ToList();
        var updatedEdges = CollectEdgesFromDiagram();
        WriteActiveGraphSlice(updatedNodes, updatedEdges);
        if (_activeSubflowId is null)
        {
            var graph = CurrentFileGraph();
            graph.Start = start;
            graph.Nodes = updatedNodes;
            graph.Edges = updatedEdges;
            if (string.Equals(_activeFlowPath, _entryFlowPath, StringComparison.OrdinalIgnoreCase))
                _project.Flow = graph;
        }
    }

    /// <summary>Validação de projeto: grafo/modelo + vínculos <c>linked_projects</c> (disco).</summary>
    void ValidateProjectGraph()
    {
        GraphValidator.ValidateProject(_project);
        if (!string.IsNullOrWhiteSpace(_projectDir))
            LinkedProjectValidator.Validate(_project, _projectDir);
    }

    void Validate()
    {
        try
        {
            SyncUiToProject();
            ValidateProjectGraph();
            SetStatus("Fluxo válido");
            MessageBox.Show("Fluxo válido.", "Validação", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (ValidationException ex)
        {
            SetStatus(ex.Message, isError: true);
            MessageBox.Show(ex.Message, "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    async Task RunAsync()
    {
        if (IsRunning)
            return;
        SyncUiToProject();
        try
        {
            ValidateProjectGraph();
        }
        catch (ValidationException ex)
        {
            MessageBox.Show(ex.Message, "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LogLines.Clear();
        _runLogFormatter.Reset();
        CopyLogCommand.RaiseCanExecuteChanged();
        ClearLogCommand.RaiseCanExecuteChanged();
        IsRunning = true;
        _runCts = new CancellationTokenSource();
        var dir = _projectDir;
        var project = _project;
        SetStatus("Executando…");

        try
        {
            var token = _runCts.Token;
            await Task.Run(() =>
            {
                FlowExecutor.RunProject(project, dir, evt =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var logLine = RunLogLine.FromEvent(evt, _runLogFormatter);
                        if (!logLine.IsHidden)
                            LogLines.Add(logLine);
                        CopyLogCommand.RaiseCanExecuteChanged();
                        ClearLogCommand.RaiseCanExecuteChanged();
                        RecordRunEvent(evt);
                        SetActiveHighlight();
                        if (evt is RunEvent.ActionExecuted)
                            SetStatus("Executando…");
                    }, DispatcherPriority.Background);
                }, token);
            }, token);
            SetStatus("Execução concluída");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Execução cancelada");
        }
        catch (Exception ex)
        {
            SetStatus(RunErrors.CliMessage(ex), isError: true);
            MessageBox.Show(RunErrors.CliMessage(ex), "Execução", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RunCancel.Reset();
            IsRunning = false;
            _runCts?.Dispose();
            _runCts = null;
        }
    }

    void StopRun()
    {
        RunCancel.Request();
        _runCts?.Cancel();
    }

    void ClearRunLog()
    {
        if (LogLines.Count == 0)
            return;
        LogLines.Clear();
        _runLogFormatter.Reset();
        CopyLogCommand.RaiseCanExecuteChanged();
        ClearLogCommand.RaiseCanExecuteChanged();
        PulseLogToolbarNotice("Log limpo.");
        SetStatus("Log de execução limpo.");
    }

    void CopyLogToClipboard()
    {
        if (LogLines.Count == 0)
            return;
        var text = string.Join(
            Environment.NewLine,
            LogLines.Select(l => ShowLogJson ? l.Json : l.Human));
        Clipboard.SetText(text);
        var mode = ShowLogJson ? "JSON" : "texto formatado";
        PulseLogToolbarNotice($"Log copiado ({LogLines.Count} linha(s), {mode}).");
        SetStatus($"Log copiado ({LogLines.Count} linha(s)).");
    }

    void PulseLogToolbarNotice(string message)
    {
        LogToolbarNotice = message;
        _logNoticeTimer?.Stop();
        _logNoticeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _logNoticeTimer.Tick += (_, _) =>
        {
            _logNoticeTimer.Stop();
            LogToolbarNotice = null;
        };
        _logNoticeTimer.Start();
    }

    void AddNodeFromPalette(string paletteKey)
    {
        if (!CanEditDiagram)
            return;
        if (paletteKey == "ui:create_subflow")
        {
            CreateSubflow();
            return;
        }
        if (paletteKey == "node:start")
        {
            AddStartNodeFromPalette();
            return;
        }
        if (paletteKey == "node:end")
        {
            AddEndNodeFromPalette();
            return;
        }
        PushHistory();
        var id = $"node_{Guid.NewGuid():N}"[..12];
        var taken = CollectTakenNames();
        var (kind, action, control) = CreateNodeFromPalette(paletteKey, taken);
        var node = new Node
        {
            Id = id,
            Kind = kind,
            Label = id,
            Action = action,
            Control = control,
        };
        var (sliceNodes, sliceEdges, _) = ActiveGraphSlice();
        node.Ui = ComputePalettePlacement(sliceNodes, SelectedNodeId, paletteKey);
        sliceNodes.Add(node);
        WriteActiveGraphSlice(sliceNodes, sliceEdges);
        if (_activeSubflowId is null)
        {
            _project.Flow.Nodes = sliceNodes;
            _project.Flow.Edges = sliceEdges;
        }
        RebuildCanvas();
        var vm = Nodes.FirstOrDefault(n => n.Id == id);
        SelectSingleNode(vm);
        OnPropertyChanged(nameof(ShowWelcomeHint));
        MarkDirty();
        SetStatus("Nó adicionado — ligue arrastando conectores no canvas.");
    }

    void AddStartNodeFromPalette()
    {
        var (sliceNodes, sliceEdges, start) = ActiveGraphSlice();
        if (sliceNodes.Any(n => n.Kind == NodeKind.Start))
        {
            SetStatus("Este fluxo já tem um nó Start.", isError: true);
            return;
        }
        PushHistory();
        var id = sliceNodes.Any(n => n.Id == "start") ? $"start_{Guid.NewGuid():N}"[..12] : "start";
        var node = new Node
        {
            Id = id,
            Kind = NodeKind.Start,
            Label = "Start",
            Ui = ComputePalettePlacement(sliceNodes, SelectedNodeId, "node:start"),
        };
        sliceNodes.Add(node);
        if (_activeSubflowId is null)
            CurrentFileGraph().Start = id;
        WriteActiveGraphSlice(sliceNodes, sliceEdges);
        RebuildCanvas();
        SelectSingleNode(Nodes.FirstOrDefault(n => n.Id == id));
        MarkDirty();
        SetStatus("Nó Start adicionado.");
    }

    void AddEndNodeFromPalette()
    {
        PushHistory();
        var (sliceNodes, sliceEdges, _) = ActiveGraphSlice();
        var id = $"end_{Guid.NewGuid():N}"[..12];
        var node = new Node
        {
            Id = id,
            Kind = NodeKind.End,
            Label = "End",
            Ui = ComputePalettePlacement(sliceNodes, SelectedNodeId, "node:end"),
        };
        sliceNodes.Add(node);
        WriteActiveGraphSlice(sliceNodes, sliceEdges);
        RebuildCanvas();
        SelectSingleNode(Nodes.FirstOrDefault(n => n.Id == id));
        MarkDirty();
        SetStatus("Nó End adicionado.");
    }

    NodeUi ComputePalettePlacement(List<Node> sliceNodes, string? selectedNodeId, string? paletteKey = null)
    {
        const double nodeW = 180;
        const double nodeH = 90;
        const double stepX = nodeW + 40;
        const double stepY = nodeH + 30;

        var occupied = new List<(double X, double Y)>(sliceNodes.Count);
        foreach (var n in sliceNodes)
        {
            var vm = Nodes.FirstOrDefault(v => v.Id == n.Id);
            occupied.Add((vm?.Location.X ?? n.Ui?.X ?? 0, vm?.Location.Y ?? n.Ui?.Y ?? 0));
        }

        bool Overlaps(double x, double y) =>
            occupied.Any(o => Math.Abs(o.X - x) < nodeW - 10 && Math.Abs(o.Y - y) < nodeH - 10);

        (double x, double y) candidate;
        if (selectedNodeId is not null && sliceNodes.Any(n => n.Id == selectedNodeId))
        {
            var anchor = sliceNodes.First(n => n.Id == selectedNodeId);
            var vm = Nodes.FirstOrDefault(v => v.Id == selectedNodeId);
            var ax = vm?.Location.X ?? anchor.Ui?.X ?? 80;
            var ay = vm?.Location.Y ?? anchor.Ui?.Y ?? 120;
            candidate = (ax + stepX, ay);
            for (var i = 0; i < 24 && Overlaps(candidate.x, candidate.y); i++)
                candidate = (candidate.x, candidate.y + stepY);
        }
        else
        {
            var key = paletteKey ?? "action:log";
            candidate = PaletteSpawn.Next(key);
            for (var attempt = 0; attempt < 48 && Overlaps(candidate.x, candidate.y); attempt++)
                candidate = PaletteSpawn.Next(key);
        }

        return new NodeUi { X = candidate.x, Y = candidate.y };
    }

    static (NodeKind kind, ActionKind? action, ControlKind? control) CreateNodeFromPalette(string key, IReadOnlySet<string>? takenNames = null)
    {
        if (key.StartsWith("action:", StringComparison.Ordinal))
        {
            var type = key["action:".Length..];
            return (NodeKind.Action, ActionDefaults.Create(type, takenNames), null);
        }
        return key switch
        {
            "if" => (NodeKind.If, null, new IfControlKind { Left = "flag", Operator = CompareOperator.Eq, Right = "yes" }),
            "switch" => (NodeKind.Switch, null, new SwitchControlKind { Expression = "{flag}", IgnoreCase = true }),
            "case" => (NodeKind.Case, null, new CaseControlKind { Value = "yes", Operator = CompareOperator.Eq }),
            "case_default" => (NodeKind.CaseDefault, null, new CaseDefaultControlKind()),
            "for_each" => (NodeKind.ForEach, null, new ForEachControlKind { ListVariable = "items", ItemVariable = "item" }),
            "while_loop" => (NodeKind.WhileLoop, null, new WhileControlKind { Left = "counter", Operator = CompareOperator.Lt, Right = "4" }),
            "repeat" => (NodeKind.Repeat, null, new RepeatControlKind { Times = "3", IndexVariable = "index" }),
            "next_loop" => (NodeKind.NextLoop, null, null),
            "exit_loop" => (NodeKind.ExitLoop, null, null),
            _ => (NodeKind.Action, new LogAction { Message = "hello" }, null),
        };
    }

    void ClearFlow()
    {
        if (MessageBox.Show("Limpar fluxo (mantém manifest)?", "Limpar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        PushHistory();
        var blank = DefaultFlow.Create();
        _project = new FlowProject
        {
            Manifest = _project.Manifest,
            Flow = blank.Flow,
            Variables = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
        };
        _variablesJson = "{}";
        OnPropertyChanged(nameof(VariablesJson));
        RebuildCanvas();
        MarkDirty();
    }

    void PushHistory()
    {
        _undoStack.Push(ProjectDigest());
        TrimHistoryStack(_undoStack);
        _redoStack.Clear();
        RaiseHistoryCommands();
    }

    void Undo()
    {
        if (_undoStack.Count == 0)
            return;
        _redoStack.Push(ProjectDigest());
        TrimHistoryStack(_redoStack);
        ApplyHistoryDigest(_undoStack.Pop());
    }

    void Redo()
    {
        if (_redoStack.Count == 0)
            return;
        _undoStack.Push(ProjectDigest());
        TrimHistoryStack(_undoStack);
        ApplyHistoryDigest(_redoStack.Pop());
    }

    void ApplyHistoryDigest(string digest)
    {
        _project = ProjectLoader.DeserializeProjectFromString(digest);
        RebuildCanvas();
        MarkDirty();
        RefreshAllInspector();
        RaiseHistoryCommands();
    }

    static void TrimHistoryStack(Stack<string> stack)
    {
        if (stack.Count <= HistoryLimit)
            return;
        var trimmed = stack.Reverse().Take(HistoryLimit).Reverse().ToList();
        stack.Clear();
        foreach (var t in trimmed)
            stack.Push(t);
    }

    void RaiseHistoryCommands()
    {
        UndoCommand.RaiseCanExecuteChanged();
        RedoCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(CanUndoDiagram));
        OnPropertyChanged(nameof(CanRedoDiagram));
        FlowCanvas?.PatchToolbarState(CanUndoDiagram, CanRedoDiagram);
    }

    void SetActiveHighlight()
    {
        foreach (var n in Nodes)
            n.IsActive = n.Id == _activeNodeId;
        NotifyFlowCanvasNodePresentation(Nodes.ToArray());
    }

    void MarkDirty()
    {
        IsDirty = ProjectDigest() != _savedDigest;
        OnPropertyChanged(nameof(DocumentTitle));
    }

    string ProjectDigest() => ProjectLoader.SerializeProjectToString(_project);

    Node? GetSelectedNode()
    {
        if (_selectedNodeId is null)
            return null;
        var (nodes, _, _) = ActiveGraphSlice();
        return nodes.FirstOrDefault(n => n.Id == _selectedNodeId);
    }

    void AddWaypoint()
    {
        if (SelectedConnection is null || !CanEditDiagram)
            return;
        PushHistory();
        var mid = new Point(
            (SelectedConnection.SourcePoint.X + SelectedConnection.TargetPoint.X) / 2,
            (SelectedConnection.SourcePoint.Y + SelectedConnection.TargetPoint.Y) / 2);
        SelectedConnection.Waypoints.Add(mid);
        SelectedConnection.NotifyGeometryChanged();
        MarkDirty();
        NotifyFlowCanvasEdge(SelectedConnection);
    }

    void NotifyFlowCanvasEdge(FlowConnectionViewModel? conn)
    {
        if (conn is null)
            NotifyFlowCanvasFull();
        else
            FlowCanvas?.PatchEdgeWaypoints(
                FlowCanvasSerializer.EdgeId(conn.Source.Id, conn.Port, conn.Target.Id),
                conn.Waypoints.Select(p => (p.X, p.Y)).ToList());
    }

    void RefreshNodeSummary(Node node)
    {
        var vm = Nodes.FirstOrDefault(n => n.Id == node.Id);
        if (vm is not null)
        {
            vm.Summary = Summarize(node);
            NotifyFlowCanvasNodePresentation(vm);
        }
    }

    static string Summarize(Node node) => node.Kind switch
    {
        NodeKind.Action => node.Action?.Type ?? "action",
        NodeKind.If => "if",
        NodeKind.Switch => "switch",
        NodeKind.Case => "case",
        NodeKind.CaseDefault => "default",
        NodeKind.ForEach => "for_each",
        NodeKind.WhileLoop => "while",
        NodeKind.Repeat => "repeat",
        _ => node.Kind.ToString().ToLowerInvariant(),
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : System.Windows.Input.ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : System.Windows.Input.ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter is T t ? t : default) ?? true;

    public void Execute(object? parameter) => execute(parameter is T t ? t : default);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
