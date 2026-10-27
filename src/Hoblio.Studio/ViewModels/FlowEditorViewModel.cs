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
    public ObservableCollection<RunLogLine> LogLines { get; } = [];
    public ObservableCollection<string> PaletteItems { get; } = BuildPalette();

    static ObservableCollection<string> BuildPalette()
    {
        var items = ActionDefaults.AllActionTypes.Select(t => "action:" + t).ToList();
        items.AddRange(["if", "for_each", "while_loop", "repeat", "next_loop", "exit_loop"]);
        return new ObservableCollection<string>(items);
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
        private set { _projectDir = value; OnPropertyChanged(); OnPropertyChanged(nameof(Title)); }
    }

    public string Title => _project.Manifest.Name + (IsDirty ? " *" : "");

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
            _selectedNodeId = value;
            OnPropertyChanged();
            RefreshAllInspector();
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
        }
    }

    public string SelectedEdgePort
    {
        get => SelectedConnection?.Port.AsString() ?? "main";
        set
        {
            if (SelectedConnection is null)
                return;
            if (EdgePortExtensions.Parse(value) is not { } port)
                return;
            SelectedConnection.Port = port;
            var edge = _project.Flow.Edges.FirstOrDefault(e =>
                e.From == SelectedConnection.Source.Id && e.To == SelectedConnection.Target.Id);
            if (edge is not null)
            {
                PushHistory();
                var idx = _project.Flow.Edges.IndexOf(edge);
                _project.Flow.Edges[idx] = new Edge
                {
                    From = edge.From,
                    To = edge.To,
                    Port = port.AsString(),
                    Label = edge.Label,
                    Ui = edge.Ui,
                };
                MarkDirty();
            }
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
                _project.Variables = doc.RootElement.EnumerateObject()
                    .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.Ordinal);
                MarkDirty();
            }
            catch
            {
                Status = "JSON de variáveis inválido";
            }
        }
    }

    public string NodeLabelEdit
    {
        get => GetSelectedNode()?.Label ?? "";
        set
        {
            var node = GetSelectedNode();
            if (node is null)
                return;
            node.Label = value;
            var vm = Nodes.FirstOrDefault(n => n.Id == node.Id);
            if (vm is not null)
                vm.Label = value;
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
            RefreshNodeSummary(node);
            MarkDirty();
            OnPropertyChanged();
        }
    }

    public string PaletteSelection { get; set; } = "action:log";

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
        AddNodeCommand = new RelayCommand(AddNodeFromPalette);
        ClearFlowCommand = new RelayCommand(ClearFlow);
        UndoCommand = new RelayCommand(Undo, () => _undoStack.Count > 0);
        RedoCommand = new RelayCommand(Redo, () => _redoStack.Count > 0);
        InitInspectorCommands();
        LoadProject(_project, null, resetUndo: true);
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
        RebuildCanvas();
        _savedDigest = ProjectDigest();
        IsDirty = false;
        OnPropertyChanged(nameof(Title));
        if (resetUndo)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            RaiseHistoryCommands();
        }
        Status = "Projeto carregado";
    }

    void RebuildCanvas()
    {
        Nodes.Clear();
        Connections.Clear();
        var vmById = new Dictionary<string, FlowNodeViewModel>();
        foreach (var node in _project.Flow.Nodes)
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
            vmById[node.Id] = vm;
            Nodes.Add(vm);
        }
        SetActiveHighlight();
        foreach (var edge in _project.Flow.Edges)
        {
            if (!vmById.TryGetValue(edge.From, out var src) || !vmById.TryGetValue(edge.To, out var tgt))
                continue;
            Connections.Add(new FlowConnectionViewModel
            {
                Source = src,
                Target = tgt,
                Port = edge.ResolvedPort(),
            });
        }
    }

    void SaveProject()
    {
        if (_projectDir is null)
        {
            MessageBox.Show("Use Abrir em uma pasta de projeto existente ou salve manualmente via Serde.", "Salvar");
            return;
        }
        SyncUiToProject();
        ProjectLoader.SaveProject(_projectDir, _project);
        _savedDigest = ProjectDigest();
        IsDirty = false;
        OnPropertyChanged(nameof(Title));
        Status = "Salvo";
    }

    void SyncUiToProject()
    {
        var uiMap = Nodes.ToDictionary(n => n.Id, n => n.Location);
        var updatedNodes = _project.Flow.Nodes.Select(node => new Node
        {
            Id = node.Id,
            Kind = node.Kind,
            Label = node.Label,
            Action = node.Action,
            Control = node.Control,
            ErrorHandling = node.ErrorHandling,
            Ui = uiMap.TryGetValue(node.Id, out var loc) ? new NodeUi { X = loc.X, Y = loc.Y } : node.Ui,
        }).ToList();
        _project = new FlowProject
        {
            Manifest = _project.Manifest,
            Flow = new FlowGraph
            {
                Start = _project.Flow.Start,
                Nodes = updatedNodes,
                Edges = _project.Flow.Edges,
            },
            Variables = _project.Variables,
        };
    }

    void Validate()
    {
        try
        {
            SyncUiToProject();
            GraphValidator.ValidateProject(_project);
            Status = "Fluxo válido";
            MessageBox.Show("Fluxo válido.", "Validação", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (ValidationException ex)
        {
            Status = ex.Message;
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
            GraphValidator.ValidateProject(_project);
        }
        catch (ValidationException ex)
        {
            MessageBox.Show(ex.Message, "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LogLines.Clear();
        IsRunning = true;
        _runCts = new CancellationTokenSource();
        var dir = _projectDir;
        var project = _project;
        Status = "Executando…";

        try
        {
            await Task.Run(() =>
            {
                FlowExecutor.RunProject(project, dir, evt =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogLines.Add(RunLogLine.FromEvent(evt));
                        RecordRunEvent(evt);
                        SetActiveHighlight();
                        if (evt is RunEvent.ActionExecuted)
                            Status = "Executando…";
                    }, DispatcherPriority.Background);
                });
            }, _runCts.Token);
            Status = "Execução concluída";
        }
        catch (OperationCanceledException)
        {
            Status = "Execução cancelada";
        }
        catch (Exception ex)
        {
            Status = RunErrors.CliMessage(ex);
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

    void AddNodeFromPalette()
    {
        PushHistory();
        var id = $"node_{Guid.NewGuid():N}"[..12];
        var (kind, action, control) = CreateNodeFromPalette(PaletteSelection);
        var node = new Node
        {
            Id = id,
            Kind = kind,
            Label = id,
            Action = action,
            Control = control,
            Ui = new NodeUi { X = 200, Y = 200 },
        };
        _project.Flow.Nodes.Add(node);
        var attachFrom = SelectedNodeId ?? _project.Flow.Start;
        _project.Flow.Edges.Add(new Edge { From = attachFrom, To = id, Port = "main" });
        RebuildCanvas();
        SelectedNodeId = id;
        MarkDirty();
    }

    static (NodeKind kind, ActionKind? action, ControlKind? control) CreateNodeFromPalette(string key)
    {
        if (key.StartsWith("action:", StringComparison.Ordinal))
        {
            var type = key["action:".Length..];
            return (NodeKind.Action, ActionDefaults.Create(type), null);
        }
        return key switch
        {
            "if" => (NodeKind.If, null, new IfControlKind { Left = "flag", Operator = CompareOperator.Eq, Right = "yes" }),
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
    }

    void SetActiveHighlight()
    {
        foreach (var n in Nodes)
            n.IsActive = n.Id == _activeNodeId;
    }

    void MarkDirty()
    {
        IsDirty = ProjectDigest() != _savedDigest;
        OnPropertyChanged(nameof(Title));
    }

    string ProjectDigest() => ProjectLoader.SerializeProjectToString(_project);

    Node? GetSelectedNode() =>
        _selectedNodeId is null ? null : _project.Flow.Nodes.FirstOrDefault(n => n.Id == _selectedNodeId);

    void RefreshNodeSummary(Node node)
    {
        var vm = Nodes.FirstOrDefault(n => n.Id == node.Id);
        if (vm is not null)
            vm.Summary = Summarize(node);
    }

    static string Summarize(Node node) => node.Kind switch
    {
        NodeKind.Action => node.Action?.Type ?? "action",
        NodeKind.If => "if",
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
