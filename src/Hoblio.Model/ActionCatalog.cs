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
        WebAutomationAction w => new("WebAutomation", WebAutomationActionTypes.TelemetryActionId(w)),
        SetVariableAction => new("Variables", "Assign"),
        IncreaseVariableAction => new("Variables", "IncreaseVariable"),
        DecreaseVariableAction => new("Variables", "DecreaseVariable"),
        ListAppendAction => new("Variables", "AddItemToList"),
        ListClearAction => new("Variables", "ClearList"),
        RunFlowAction => new("FlowControl", "CallFunction"),
        ExcelAction e => new("Excel", ExcelActionTypes.ModuleActionId(e.Type)),
        WordAction w => new("Word", WordActionTypes.ModuleActionId(w.Type)),
        VariablesAction v => new("Variables", VariablesActionTypes.ModuleActionId(v.Type)),
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
            WebAutomationAction w when w.ProducedVariables().Any() => (null, w.ProducedVariables().ToList()),
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
            VariablesAction va => VariablesVariableIo(va),
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

    static (IReadOnlyList<string>? Inputs, IReadOnlyList<string>? Outputs) VariablesVariableIo(VariablesAction va)
    {
        var inputs = new List<string>();
        if (!string.IsNullOrWhiteSpace(va.DataTableVariable))
            inputs.Add(va.DataTableVariable.Trim());
        if (!string.IsNullOrWhiteSpace(va.SecondDataTableVariable))
            inputs.Add(va.SecondDataTableVariable.Trim());
        if (!string.IsNullOrWhiteSpace(va.ListVariable))
            inputs.Add(va.ListVariable.Trim());
        if (!string.IsNullOrWhiteSpace(va.SecondListVariable))
            inputs.Add(va.SecondListVariable.Trim());
        if (!string.IsNullOrWhiteSpace(va.SourceVariable))
            inputs.Add(va.SourceVariable.Trim());

        var outputs = va.ProducedVariables().ToList();
        if (outputs.Count == 0 && !string.IsNullOrWhiteSpace(va.DataTableVariable)
            && va.Type is not VariablesActionTypes.CreateDatatable and not VariablesActionTypes.ReadCsv)
            outputs.Add(va.DataTableVariable.Trim());
        if (outputs.Count == 0 && !string.IsNullOrWhiteSpace(va.ListVariable)
            && va.Type != VariablesActionTypes.CreateList)
            outputs.Add(va.ListVariable.Trim());

        return (inputs.Count == 0 ? null : inputs, outputs.Count == 0 ? null : outputs);
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
