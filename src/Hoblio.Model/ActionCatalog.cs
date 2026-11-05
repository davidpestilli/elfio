namespace Hoblio.Model;

public readonly record struct ActionDescriptor(string Module, string ActionId);

/// <summary>Stable module/action identifiers for telemetry and manifest capabilities.</summary>
public static class ActionCatalog
{
    public static ActionDescriptor Describe(Node node) => node.Kind switch
    {
        NodeKind.Action when node.Action is not null => Describe(node.Action),
        NodeKind.If => new("Conditionals", "If"),
        NodeKind.Switch => new("Conditionals", "Switch"),
        NodeKind.Case => new("Conditionals", "Case"),
        NodeKind.CaseDefault => new("Conditionals", "CaseDefault"),
        NodeKind.ForEach => new("Loops", "Foreach"),
        NodeKind.WhileLoop => new("Loops", "While"),
        NodeKind.Repeat => new("Loops", "Loop"),
        NodeKind.NextLoop => new("Loops", "Continue"),
        NodeKind.ExitLoop => new("Loops", "Break"),
        NodeKind.Start => new("FlowControl", "Start"),
        NodeKind.End => new("FlowControl", "End"),
        _ => new("System", "Unknown"),
    };

    public static ActionDescriptor Describe(ActionKind action) => action switch
    {
        LogAction => new("System", "LogMessage"),
        NoOpAction => new("System", "NoOp"),
        DelayAction => new("System", "Wait"),
        HttpGetAction => new("Web", "InvokeWebServiceBase"),
        ShowMessageAction => new("Display", "ShowMessage"),
        ShowGetUserInputAction => new("Display", "DisplayInputDialog"),
        ShowSelectFromListAction => new("Display", "SelectFromList"),
        FileWriteAction => new("File", "WriteText"),
        FileAppendAction => new("File", "AppendLine"),
        FileReadAction => new("File", "ReadText"),
        BrowserNavigateAction => new("WebAutomation", "LaunchChromeBase"),
        BrowserClickAction => new("WebAutomation", "ClickBase"),
        BrowserWaitNavigationAction => new("WebAutomation", "WaitForPageToLoadBase"),
        BrowserFillAction => new("WebAutomation", "PopulateTextFieldBase"),
        BrowserWaitForAction => new("WebAutomation", "WaitForElementBase"),
        BrowserGetTextAction => new("WebAutomation", "GetDetailsOfElementBase"),
        SetVariableAction => new("Variables", "Assign"),
        IncreaseVariableAction => new("Variables", "IncreaseVariable"),
        DecreaseVariableAction => new("Variables", "DecreaseVariable"),
        ListAppendAction => new("Variables", "AddItemToList"),
        ListClearAction => new("Variables", "ClearList"),
        RunFlowAction => new("FlowControl", "CallFunction"),
        ExcelAction e => new("Excel", ExcelActionTypes.ModuleActionId(e.Type)),
        WordAction w => new("Word", WordActionTypes.ModuleActionId(w.Type)),
        _ => new("System", action.Type),
    };

    public static void CollectModulesFromGraph(FlowGraph graph, ISet<string> modules)
    {
        foreach (var node in graph.Nodes)
        {
            if (node.Kind is NodeKind.Start or NodeKind.End)
                continue;
            modules.Add(Describe(node).Module);
        }

        if (graph.Subflows is null)
            return;
        foreach (var bundle in graph.Subflows.Values)
            CollectModulesFromGraph(bundle.AsGraph(null), modules);
    }

    public static (IReadOnlyList<string>? Inputs, IReadOnlyList<string>? Outputs) VariableIo(ActionKind action) =>
        action switch
        {
            FileReadAction fr => (null, [fr.Variable]),
            FileWriteAction fw => ([nameof(fw.Path)], null),
            FileAppendAction fa => ([nameof(fa.Path)], null),
            HttpGetAction h when !string.IsNullOrWhiteSpace(h.Variable) => (null, [h.Variable.Trim()]),
            BrowserGetTextAction gt when !string.IsNullOrEmpty(gt.Variable) => (null, [gt.Variable]),
            SetVariableAction sv => (null, [sv.Variable]),
            ListAppendAction la => ([la.ListVariable], [la.ListVariable]),
            ListClearAction lc => ([lc.ListVariable], [lc.ListVariable]),
            ShowGetUserInputAction ui => (null, FilterNames(ui.UserInputVariable, ui.ButtonPressedVariable)),
            ShowSelectFromListAction sl => (null, FilterNames(sl.SelectedItemVariable, sl.SelectedIndexVariable, sl.ButtonPressedVariable)),
            RunFlowAction rf => (rf.Inputs?.Keys.ToList(), rf.Outputs),
            ExcelAction ex => ExcelVariableIo(ex),
            WordAction w => WordVariableIo(w),
            IncreaseVariableAction iv => ([iv.Variable], [iv.Variable]),
            DecreaseVariableAction dv => ([dv.Variable], [dv.Variable]),
            _ => (null, null),
        };

    static List<string>? FilterNames(params string?[] names)
    {
        var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!.Trim()).ToList();
        return list.Count == 0 ? null : list;
    }

    static (IReadOnlyList<string>? Inputs, IReadOnlyList<string>? Outputs) ExcelVariableIo(ExcelAction ex)
    {
        if (ex.Type == ExcelActionTypes.Launch)
            return (null, ex.ProducedVariables().ToList() is { Count: > 0 } o ? o : null);

        var inputs = new List<string> { ex.Instance };
        if (ex.Type == ExcelActionTypes.CopyWorksheet
            && !string.IsNullOrWhiteSpace(ex.TargetInstance)
            && !string.Equals(ex.TargetInstance.Trim(), ex.Instance.Trim(), StringComparison.Ordinal))
            inputs.Add(ex.TargetInstance.Trim());

        var outputs = ex.ProducedVariables().ToList();
        return (inputs, outputs.Count == 0 ? null : outputs);
    }

    static (IReadOnlyList<string>? Inputs, IReadOnlyList<string>? Outputs) WordVariableIo(WordAction w)
    {
        if (w.Type == WordActionTypes.Launch)
            return (null, w.ProducedVariables().ToList() is { Count: > 0 } o ? o : null);

        var inputs = new List<string> { w.Instance };
        var outputs = w.ProducedVariables().ToList();
        return (inputs, outputs.Count == 0 ? null : outputs);
    }
}
