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

    public bool InspectorReadOnly => !IsEditing || IsRunning;

    public bool ShowActionSection => GetSelectedNode()?.Kind == NodeKind.Action;
    public bool ShowIfSection => GetSelectedNode()?.Kind == NodeKind.If;
    public bool ShowSwitchSection => GetSelectedNode()?.Kind == NodeKind.Switch;
    public bool ShowCaseSection => GetSelectedNode()?.Kind == NodeKind.Case;
    public bool ShowCaseDefaultSection => GetSelectedNode()?.Kind == NodeKind.CaseDefault;
    public bool ShowForEachSection => GetSelectedNode()?.Kind == NodeKind.ForEach;
    public bool ShowWhileSection => GetSelectedNode()?.Kind == NodeKind.WhileLoop;
    public bool ShowRepeatSection => GetSelectedNode()?.Kind == NodeKind.Repeat;
    public bool ShowActionErrorSection => GetSelectedNode()?.Kind == NodeKind.Action;

    public bool ShowGenericFields => false;
    public bool ShowGenericField2 => ShowGenericFields && !string.IsNullOrWhiteSpace(GenericField2Label);
    public bool ShowGenericField3 => ShowGenericFields && !string.IsNullOrWhiteSpace(GenericField3Label);

    public bool ShowRunFlowManifestPicker =>
        GetSelectedNode()?.Action is RunFlowAction && RunFlowTargetIds.Count > 0;

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

    public string SwitchExpression
    {
        get => GetSwitch()?.Expression ?? "";
        set => PatchSwitch(c => c with { Expression = value });
    }

    public bool SwitchIgnoreCase
    {
        get => GetSwitch()?.IgnoreCase ?? false;
        set => PatchSwitch(c => c with { IgnoreCase = value });
    }

    public string CaseValue
    {
        get => GetCase()?.Value ?? "";
        set => PatchCase(c => c with { Value = value });
    }

    public string CaseOperator
    {
        get => GetCase()?.Operator.ToString().ToLowerInvariant() ?? "eq";
        set
        {
            if (Enum.TryParse<CompareOperator>(value, true, out var op))
                PatchCase(c => c with { Operator = op });
        }
    }

    public bool WhileIgnoreCase
    {
        get => GetWhile()?.IgnoreCase ?? false;
        set => PatchWhile(c => c with { IgnoreCase = value });
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

    public string SelectedEdgeTargetId
    {
        get => SelectedConnection?.Target.Id ?? "";
        set
        {
            if (SelectedConnection is null || string.IsNullOrWhiteSpace(value) || value == SelectedConnection.Target.Id)
                return;
            var newTo = value.Trim();
            if (Nodes.All(n => n.Id != newTo))
                return;
            PatchSelectedEdge((edge, idx, edges) =>
            {
                edges[idx] = new Edge
                {
                    Id = edge.Id,
                    From = edge.From,
                    To = newTo,
                    Port = edge.Port,
                    Label = edge.Label,
                    Ui = edge.Ui,
                };
            });
            OnPropertyChanged();
            SetStatus($"Destino atualizado → `{newTo}`.");
        }
    }

    public RelayCommand DeleteNodeCommand { get; private set; } = null!;
    public RelayCommand RemoveEdgeCommand { get; private set; } = null!;

    void InitInspectorCommands()
    {
        DeleteNodeCommand = new RelayCommand(DeleteSelectedNode, () => CanDeleteSelectedNode());
        RemoveEdgeCommand = new RelayCommand(RemoveSelectedEdge, () => SelectedConnection is not null);
    }

    bool CanDeleteSelectedNode()
    {
        var node = GetSelectedNode();
        return node is not null && node.Kind is not (NodeKind.Start or NodeKind.End);
    }

    void DeleteSelectedNode() => DeleteSelectedNodes();

    void RemoveSelectedEdge() => OnRemoveConnection(SelectedConnection);

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
        OnPropertyChanged(nameof(SwitchExpression));
        OnPropertyChanged(nameof(SwitchIgnoreCase));
        OnPropertyChanged(nameof(CaseValue));
        OnPropertyChanged(nameof(CaseOperator));
        OnPropertyChanged(nameof(WhileIgnoreCase));
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
        OnPropertyChanged(nameof(NodeLabelEdit));
        RefreshActionFieldBindings();
        OnPropertyChanged(nameof(LastRunVariablesText));
        OnPropertyChanged(nameof(ShowRunFlowManifestPicker));
        OnPropertyChanged(nameof(ShowRunFlowSection));
        OnPropertyChanged(nameof(ShowRunFlowOpenSubflow));
        OnPropertyChanged(nameof(RunFlowId));
        OnPropertyChanged(nameof(RunFlowPath));
        OnPropertyChanged(nameof(RunFlowScopeMode));
        OnPropertyChanged(nameof(RunFlowShareBrowser));
        OnPropertyChanged(nameof(RunFlowInputsText));
        OnPropertyChanged(nameof(RunFlowOutputsText));
        OpenSelectedRunFlowSubflowCommand?.RaiseCanExecuteChanged();
        RefreshInspectorVisibility();
    }

    HashSet<string> CollectTakenNames()
    {
        var (nodes, _, _) = ActiveGraphSlice();
        var seeds = ProjectScalars.Select(s => s.Name).Where(n => !string.IsNullOrWhiteSpace(n));
        return FlowOccupiedVariableNames.Collect(nodes, seeds);
    }

    IfControl? GetIf()
    {
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.If || node.Control is null)
            return null;
        return node.Control.AsIf();
    }

    SwitchControl? GetSwitch()
    {
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.Switch || node.Control is null)
            return null;
        return node.Control.AsSwitch();
    }

    CaseControl? GetCase()
    {
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.Case || node.Control is null)
            return null;
        return node.Control.AsCase();
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

    void PatchSwitch(Func<SwitchControl, SwitchControl> patch)
    {
        var node = GetSelectedNode();
        if (node?.Control is not SwitchControlKind c)
            return;
        var next = patch(c.AsSwitch());
        node.Control = new SwitchControlKind
        {
            Expression = next.Expression,
            IgnoreCase = next.IgnoreCase,
        };
        MarkDirty();
        RefreshNodeSummary(node);
        RefreshAllInspector();
    }

    void PatchCase(Func<CaseControl, CaseControl> patch)
    {
        var node = GetSelectedNode();
        if (node?.Control is not CaseControlKind c)
            return;
        var next = patch(c.AsCase());
        node.Control = new CaseControlKind
        {
            Value = next.Value,
            Operator = next.Operator,
        };
        MarkDirty();
        RefreshNodeSummary(node);
        RefreshAllInspector();
    }

    void PatchForEach(Func<ForEachControl, ForEachControl> patch)
    {
        var node = GetSelectedNode();
        if (node?.Control is not ForEachControlKind c)
            return;
        var next = patch(c.AsForEach());
        CommitForEachWithOptionalProducerRename(c.ItemVariable, next);
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
        CommitRepeatWithOptionalProducerRename(c.IndexVariable, next);
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
        RunFlowAction => ("flow_id", "path (alternativo)", "outputs (vírgula)"),
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
        RunFlowAction a => index switch
        {
            1 => a.FlowId ?? "",
            2 => a.Path ?? "",
            3 => a.Outputs is null ? "" : string.Join(", ", a.Outputs),
            _ => "",
        },
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
            RunFlowAction a => new RunFlowAction
            {
                FlowId = index == 1 ? value : a.FlowId,
                Path = index == 2 ? value : a.Path,
                Outputs = index == 3
                    ? value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                    : a.Outputs,
                Inputs = a.Inputs,
                ShareBrowser = a.ShareBrowser,
                VariableScope = a.VariableScope,
            },
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
