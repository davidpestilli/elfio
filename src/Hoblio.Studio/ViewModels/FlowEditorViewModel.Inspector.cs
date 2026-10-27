using System.Collections.ObjectModel;
using Hoblio.Model;
using Hoblio.Model.Json;
using Hoblio.Runtime;
using Hoblio.Studio;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    readonly Dictionary<string, Dictionary<string, string>> _lastRunByNode = new(StringComparer.Ordinal);
    string? _activeNodeId;

    public ObservableCollection<string> ActionTypeOptions { get; } = new(ActionDefaults.AllActionTypes);
    public ObservableCollection<string> CompareOperators { get; } =
    ["eq", "ne", "contains", "gt", "gte", "lt", "lte"];

    public string? ActiveNodeId
    {
        get => _activeNodeId;
        private set { _activeNodeId = value; OnPropertyChanged(); }
    }

    public string ProducerOverview
    {
        get
        {
            var lines = new List<string>();
            foreach (var node in _project.Flow.Nodes)
            {
                if (node.Action is { } action)
                {
                    foreach (var v in action.ProducedVariables())
                        lines.Add($"{v} ← {node.Id} ({action.Type})");
                }
                if (node.Kind == NodeKind.ForEach && node.Control is ForEachControlKind fe)
                    lines.Add($"{fe.ItemVariable} ← {node.Id} (for_each item)");
                if (node.Kind == NodeKind.Repeat && node.Control is RepeatControlKind rp)
                    lines.Add($"{rp.IndexVariable} ← {node.Id} (repeat index)");
            }
            return lines.Count == 0 ? "(nenhuma)" : string.Join(Environment.NewLine, lines);
        }
    }

    public string LastRunVariablesText
    {
        get
        {
            if (_selectedNodeId is null || !_lastRunByNode.TryGetValue(_selectedNodeId, out var map))
                return "";
            return string.Join(Environment.NewLine, map.Select(kv => $"{kv.Key} = {kv.Value}"));
        }
    }

    public string SelectedNodeKind => GetSelectedNode()?.Kind.ToString() ?? "";

    public string ActionType
    {
        get => GetSelectedNode()?.Action?.Type ?? "";
        set
        {
            var node = GetSelectedNode();
            if (node?.Kind != NodeKind.Action || string.IsNullOrEmpty(value))
                return;
            PushHistory();
            var taken = CollectTakenNames();
            node.Action = ActionDefaults.Create(value, taken);
            RefreshNodeSummary(node);
            MarkDirty();
            RefreshAllInspector();
        }
    }

    public string IfLeft
    {
        get => GetIf()?.Left ?? "";
        set => PatchIf(c => c with { Left = value });
    }

    public string IfRight
    {
        get => GetIf()?.Right ?? "";
        set => PatchIf(c => c with { Right = value });
    }

    public string IfOperator
    {
        get => GetIf()?.Operator.ToString().ToLowerInvariant() ?? "eq";
        set
        {
            if (Enum.TryParse<CompareOperator>(value, true, out var op))
                PatchIf(c => c with { Operator = op });
        }
    }

    public bool IfIgnoreCase
    {
        get => GetIf()?.IgnoreCase ?? false;
        set => PatchIf(c => c with { IgnoreCase = value });
    }

    public string ForEachListVariable
    {
        get => GetForEach()?.ListVariable ?? "";
        set => PatchForEach(c => c with { ListVariable = value });
    }

    public string ForEachItemVariable
    {
        get => GetForEach()?.ItemVariable ?? "";
        set => PatchForEach(c => c with { ItemVariable = value });
    }

    public string WhileLeft
    {
        get => GetWhile()?.Left ?? "";
        set => PatchWhile(c => c with { Left = value });
    }

    public string WhileRight
    {
        get => GetWhile()?.Right ?? "";
        set => PatchWhile(c => c with { Right = value });
    }

    public string WhileOperator
    {
        get => GetWhile()?.Operator.ToString().ToLowerInvariant() ?? "lt";
        set
        {
            if (Enum.TryParse<CompareOperator>(value, true, out var op))
                PatchWhile(c => c with { Operator = op });
        }
    }

    public string RepeatTimes
    {
        get => GetRepeat()?.Times ?? "";
        set => PatchRepeat(c => c with { Times = value });
    }

    public string RepeatIndexVariable
    {
        get => GetRepeat()?.IndexVariable ?? "";
        set => PatchRepeat(c => c with { IndexVariable = value });
    }

    public string RetryCountText
    {
        get => (GetSelectedNode()?.ErrorHandling?.RetryCount ?? 0).ToString();
        set
        {
            if (!uint.TryParse(value, out var n))
                return;
            PatchErrorHandling(e => e.RetryCount = n);
        }
    }

    public string RetryDelayText
    {
        get => (GetSelectedNode()?.ErrorHandling?.RetryDelayMs ?? 500).ToString();
        set
        {
            if (!ulong.TryParse(value, out var n))
                return;
            PatchErrorHandling(e => e.RetryDelayMs = n);
        }
    }

    public bool OnFailureContinue
    {
        get => GetSelectedNode()?.ErrorHandling?.OnFailure == OnFailure.Continue;
        set => PatchErrorHandling(e => e.OnFailure = value ? OnFailure.Continue : OnFailure.Stop);
    }

    public string GenericField1
    {
        get => ReadGeneric(1);
        set => WriteGeneric(1, value);
    }

    public string GenericField2
    {
        get => ReadGeneric(2);
        set => WriteGeneric(2, value);
    }

    public string GenericField3
    {
        get => ReadGeneric(3);
        set => WriteGeneric(3, value);
    }

    public string GenericField1Label => GenericLabels().label1;
    public string GenericField2Label => GenericLabels().label2;
    public string GenericField3Label => GenericLabels().label3;

    public string NewEdgeFromId { get; set; } = "";
    public string NewEdgeToId { get; set; } = "";
    public string NewEdgePort { get; set; } = "main";

    public RelayCommand DeleteNodeCommand { get; private set; } = null!;
    public RelayCommand AddEdgeCommand { get; private set; } = null!;
    public RelayCommand RemoveEdgeCommand { get; private set; } = null!;

    void InitInspectorCommands()
    {
        DeleteNodeCommand = new RelayCommand(DeleteSelectedNode, () => CanDeleteSelectedNode());
        AddEdgeCommand = new RelayCommand(AddEdge);
        RemoveEdgeCommand = new RelayCommand(RemoveSelectedEdge, () => SelectedConnection is not null);
    }

    bool CanDeleteSelectedNode()
    {
        var node = GetSelectedNode();
        return node is not null && node.Kind is not (NodeKind.Start or NodeKind.End);
    }

    void DeleteSelectedNode()
    {
        var node = GetSelectedNode();
        if (node is null || node.Kind is NodeKind.Start or NodeKind.End)
            return;
        PushHistory();
        _project.Flow.Nodes.RemoveAll(n => n.Id == node.Id);
        _project.Flow.Edges.RemoveAll(e => e.From == node.Id || e.To == node.Id);
        SelectedNodeId = null;
        RebuildCanvas();
        MarkDirty();
    }

    void AddEdge()
    {
        if (string.IsNullOrWhiteSpace(NewEdgeFromId) || string.IsNullOrWhiteSpace(NewEdgeToId))
            return;
        PushHistory();
        _project.Flow.Edges.Add(new Edge
        {
            From = NewEdgeFromId.Trim(),
            To = NewEdgeToId.Trim(),
            Port = NewEdgePort.Trim(),
        });
        RebuildCanvas();
        MarkDirty();
    }

    void RemoveSelectedEdge()
    {
        if (SelectedConnection is null)
            return;
        PushHistory();
        var from = SelectedConnection.Source.Id;
        var to = SelectedConnection.Target.Id;
        var port = SelectedConnection.Port.AsString();
        var idx = _project.Flow.Edges.FindIndex(e =>
            e.From == from && e.To == to && string.Equals(e.Port, port, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
            _project.Flow.Edges.RemoveAt(idx);
        SelectedConnection = null;
        RebuildCanvas();
        MarkDirty();
    }

    void RecordRunEvent(RunEvent evt)
    {
        switch (evt)
        {
            case RunEvent.NodeEnter e:
                ActiveNodeId = e.NodeId;
                break;
            case RunEvent.ActionExecuted e when e.Variables is { Count: > 0 }:
                _lastRunByNode[e.NodeId] = new Dictionary<string, string>(e.Variables, StringComparer.Ordinal);
                OnPropertyChanged(nameof(LastRunVariablesText));
                break;
            case RunEvent.RunFinished:
                ActiveNodeId = null;
                break;
            case RunEvent.RunStarted:
                _lastRunByNode.Clear();
                OnPropertyChanged(nameof(LastRunVariablesText));
                break;
        }
    }

    void RefreshAllInspector()
    {
        OnPropertyChanged(nameof(SelectedNodeKind));
        OnPropertyChanged(nameof(ActionType));
        OnPropertyChanged(nameof(IfLeft));
        OnPropertyChanged(nameof(IfRight));
        OnPropertyChanged(nameof(IfOperator));
        OnPropertyChanged(nameof(IfIgnoreCase));
        OnPropertyChanged(nameof(ForEachListVariable));
        OnPropertyChanged(nameof(ForEachItemVariable));
        OnPropertyChanged(nameof(WhileLeft));
        OnPropertyChanged(nameof(WhileRight));
        OnPropertyChanged(nameof(WhileOperator));
        OnPropertyChanged(nameof(RepeatTimes));
        OnPropertyChanged(nameof(RepeatIndexVariable));
        OnPropertyChanged(nameof(RetryCountText));
        OnPropertyChanged(nameof(RetryDelayText));
        OnPropertyChanged(nameof(OnFailureContinue));
        OnPropertyChanged(nameof(GenericField1));
        OnPropertyChanged(nameof(GenericField2));
        OnPropertyChanged(nameof(GenericField3));
        OnPropertyChanged(nameof(GenericField1Label));
        OnPropertyChanged(nameof(GenericField2Label));
        OnPropertyChanged(nameof(GenericField3Label));
        OnPropertyChanged(nameof(ProducerOverview));
        OnPropertyChanged(nameof(NodeLabelEdit));
        OnPropertyChanged(nameof(LogMessageEdit));
        OnPropertyChanged(nameof(LastRunVariablesText));
    }

    HashSet<string> CollectTakenNames()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in _project.Flow.Nodes)
        {
            if (node.Action is { } a)
                foreach (var v in a.ProducedVariables())
                    set.Add(v);
        }
        return set;
    }

    IfControl? GetIf()
    {
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.If || node.Control is null)
            return null;
        return node.Control.AsIf();
    }

    ForEachControl? GetForEach()
    {
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.ForEach || node.Control is null)
            return null;
        return node.Control.AsForEach();
    }

    IfControl? GetWhile()
    {
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.WhileLoop || node.Control is null)
            return null;
        return node.Control.AsIf();
    }

    RepeatControl? GetRepeat()
    {
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.Repeat || node.Control is null)
            return null;
        return node.Control.AsRepeat();
    }

    void PatchIf(Func<IfControl, IfControl> patch)
    {
        var node = GetSelectedNode();
        if (node?.Control is not IfControlKind c)
            return;
        var next = patch(c.AsIf());
        node.Control = new IfControlKind
        {
            Left = next.Left,
            Right = next.Right,
            Operator = next.Operator,
            IgnoreCase = next.IgnoreCase,
        };
        MarkDirty();
        RefreshAllInspector();
    }

    void PatchForEach(Func<ForEachControl, ForEachControl> patch)
    {
        var node = GetSelectedNode();
        if (node?.Control is not ForEachControlKind c)
            return;
        var next = patch(c.AsForEach());
        node.Control = new ForEachControlKind { ListVariable = next.ListVariable, ItemVariable = next.ItemVariable };
        MarkDirty();
        RefreshAllInspector();
    }

    void PatchWhile(Func<IfControl, IfControl> patch)
    {
        var node = GetSelectedNode();
        if (node?.Control is not WhileControlKind c)
            return;
        var next = patch(c.AsIf());
        node.Control = new WhileControlKind
        {
            Left = next.Left,
            Right = next.Right,
            Operator = next.Operator,
            IgnoreCase = next.IgnoreCase,
        };
        MarkDirty();
        RefreshAllInspector();
    }

    void PatchRepeat(Func<RepeatControl, RepeatControl> patch)
    {
        var node = GetSelectedNode();
        if (node?.Control is not RepeatControlKind c)
            return;
        var next = patch(c.AsRepeat());
        node.Control = new RepeatControlKind { Times = next.Times, IndexVariable = next.IndexVariable };
        MarkDirty();
        RefreshAllInspector();
    }

    void PatchErrorHandling(Action<ErrorHandling> patch)
    {
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.Action)
            return;
        node.ErrorHandling ??= new ErrorHandling();
        patch(node.ErrorHandling);
        MarkDirty();
        RefreshAllInspector();
    }

    (string label1, string label2, string label3) GenericLabels() => GetSelectedNode()?.Action switch
    {
        BrowserNavigateAction => ("URL", "wait_ms", "show_on_screen (true/false)"),
        BrowserClickAction => ("selector", "timeout_ms", "wait_for_navigation (true/false)"),
        BrowserFillAction => ("selector", "text", "timeout_ms"),
        BrowserWaitForAction => ("selector", "timeout_ms", ""),
        BrowserGetTextAction => ("selector", "variable", "timeout_ms"),
        SetVariableAction => ("variable", "value", ""),
        FileWriteAction => ("path", "content", ""),
        FileReadAction => ("path", "variable", ""),
        DelayAction => ("milliseconds", "", ""),
        HttpGetAction => ("url", "", ""),
        ShowMessageAction => ("title", "text", "timeout_seconds"),
        _ => ("campo 1", "campo 2", "campo 3"),
    };

    string ReadGeneric(int index) => GetSelectedNode()?.Action switch
    {
        BrowserNavigateAction a => index switch { 1 => a.Url, 2 => a.WaitMs?.ToString() ?? "", 3 => a.ShowOnScreen?.ToString() ?? "", _ => "" },
        BrowserClickAction a => index switch { 1 => a.Selector, 2 => a.TimeoutMs?.ToString() ?? "", 3 => a.WaitForNavigation?.ToString() ?? "", _ => "" },
        BrowserFillAction a => index switch { 1 => a.Selector, 2 => a.Text, 3 => a.TimeoutMs?.ToString() ?? "", _ => "" },
        BrowserWaitForAction a => index switch { 1 => a.Selector, 2 => a.TimeoutMs?.ToString() ?? "", _ => "" },
        BrowserGetTextAction a => index switch { 1 => a.Selector, 2 => a.Variable ?? "", 3 => a.TimeoutMs?.ToString() ?? "", _ => "" },
        SetVariableAction a => index switch { 1 => a.Variable, 2 => a.Value, _ => "" },
        FileWriteAction a => index switch { 1 => a.Path, 2 => a.Content, _ => "" },
        FileReadAction a => index switch { 1 => a.Path, 2 => a.Variable, _ => "" },
        DelayAction a => index switch { 1 => a.Milliseconds.ToString(), _ => "" },
        HttpGetAction a => index switch { 1 => a.Url, _ => "" },
        ShowMessageAction a => index switch { 1 => a.Title, 2 => a.Text, 3 => a.TimeoutSeconds?.ToString() ?? "", _ => "" },
        _ => "",
    };

    void WriteGeneric(int index, string value)
    {
        var node = GetSelectedNode();
        if (node?.Action is null)
            return;
        node.Action = node.Action switch
        {
            BrowserNavigateAction a => new BrowserNavigateAction
            {
                Url = index == 1 ? value : a.Url,
                WaitMs = index == 2 && ulong.TryParse(value, out var w) ? w : a.WaitMs,
                ShowOnScreen = index == 3 && bool.TryParse(value, out var b) ? b : a.ShowOnScreen,
                StartMaximized = a.StartMaximized,
            },
            BrowserClickAction a => new BrowserClickAction
            {
                Selector = index == 1 ? value : a.Selector,
                TimeoutMs = index == 2 && ulong.TryParse(value, out var t1) ? t1 : a.TimeoutMs,
                WaitForNavigation = index == 3 && bool.TryParse(value, out var b1) ? b1 : a.WaitForNavigation,
                WaitSelectorAfter = a.WaitSelectorAfter,
                WaitAfterTimeoutMs = a.WaitAfterTimeoutMs,
            },
            BrowserFillAction a => new BrowserFillAction
            {
                Selector = index == 1 ? value : a.Selector,
                Text = index == 2 ? value : a.Text,
                TimeoutMs = index == 3 && ulong.TryParse(value, out var t2) ? t2 : a.TimeoutMs,
            },
            BrowserWaitForAction a => new BrowserWaitForAction
            {
                Selector = index == 1 ? value : a.Selector,
                TimeoutMs = index == 2 && ulong.TryParse(value, out var t3) ? t3 : a.TimeoutMs,
            },
            BrowserGetTextAction a => new BrowserGetTextAction
            {
                Selector = index == 1 ? value : a.Selector,
                Variable = index == 2 ? value : a.Variable,
                TimeoutMs = index == 3 && ulong.TryParse(value, out var t4) ? t4 : a.TimeoutMs,
            },
            SetVariableAction a => new SetVariableAction
            {
                Variable = index == 1 ? value : a.Variable,
                Value = index == 2 ? value : a.Value,
            },
            FileWriteAction a => new FileWriteAction
            {
                Path = index == 1 ? value : a.Path,
                Content = index == 2 ? value : a.Content,
            },
            FileReadAction a => new FileReadAction
            {
                Path = index == 1 ? value : a.Path,
                Variable = index == 2 ? value : a.Variable,
            },
            DelayAction a when index == 1 && ulong.TryParse(value, out var ms) => new DelayAction { Milliseconds = ms },
            DelayAction a => a,
            HttpGetAction a when index == 1 => new HttpGetAction { Url = value },
            HttpGetAction a => a,
            ShowMessageAction a => new ShowMessageAction
            {
                Title = index == 1 ? value : a.Title,
                Text = index == 2 ? value : a.Text,
                TimeoutSeconds = index == 3 && double.TryParse(value, out var s) ? s : a.TimeoutSeconds,
            },
            _ => node.Action,
        };
        RefreshNodeSummary(node);
        MarkDirty();
        RefreshAllInspector();
    }
}
