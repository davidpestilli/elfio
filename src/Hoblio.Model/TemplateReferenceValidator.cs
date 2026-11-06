using System.Text.RegularExpressions;

namespace Hoblio.Model;

public static class TemplateReferenceValidator
{
    static readonly Regex RefPattern = new(@"\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.CultureInvariant);

    public static void Validate(FlowProject project)
    {
        var known = new HashSet<string>(project.Variables.Keys, StringComparer.Ordinal);
        AddBuiltInVariables(known);
        AddLinkedProjectExports(project, known);
        foreach (var node in EnumerateNodes(project.Flow))
        {
            foreach (var name in CollectProducerNames(node))
                known.Add(name);
        }

        foreach (var node in EnumerateNodes(project.Flow))
        {
            foreach (var text in CollectTemplateStrings(node))
            {
                foreach (Match m in RefPattern.Matches(text))
                {
                    var refName = m.Groups[1].Value;
                    if (!known.Contains(refName))
                    {
                        throw new ValidationException(
                            ValidationErrorKind.UnknownTemplateReference,
                            $"node `{node.Id}` references unknown variable `{{{refName}}}` (not in project variables, flow producers or linked project exports)");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Variáveis publicadas pelo runtime (<see cref="BuiltInVariables"/>) podem ser referenciadas
    /// sem constar em <c>variables</c> — o run as escreve antes do primeiro nó.
    /// </summary>
    static void AddBuiltInVariables(HashSet<string> known)
    {
        foreach (var name in BuiltInVariables.All)
            known.Add(name);
    }

    /// <summary>
    /// Variáveis entregues por projetos vinculados (<c>manifest.linked_projects[].exports</c>) passam a ser
    /// referenciáveis no fluxo chamador — é o contrato de herança entre fluxos principais.
    /// </summary>
    static void AddLinkedProjectExports(FlowProject project, HashSet<string> known)
    {
        if (project.Manifest.LinkedProjects is null)
            return;
        foreach (var link in project.Manifest.LinkedProjects.Values)
        {
            if (link.Exports is null)
                continue;
            foreach (var name in link.Exports)
            {
                if (!string.IsNullOrWhiteSpace(name))
                    known.Add(name.Trim());
            }
        }
    }

    static IEnumerable<Node> EnumerateNodes(FlowGraph graph)
    {
        foreach (var node in graph.Nodes)
            yield return node;
        if (graph.Subflows is null)
            yield break;
        foreach (var bundle in graph.Subflows.Values)
        {
            foreach (var node in EnumerateNodes(bundle.AsGraph(null)))
                yield return node;
        }
    }

    static IEnumerable<string> CollectProducerNames(Node node)
    {
        if (node.Action is { } action)
        {
            foreach (var v in action.ProducedVariables())
            {
                if (!string.IsNullOrWhiteSpace(v))
                    yield return v.Trim();
            }
        }

        if (node.Kind == NodeKind.ForEach && node.Control is ForEachControlKind fe && !string.IsNullOrWhiteSpace(fe.ItemVariable))
            yield return fe.ItemVariable.Trim();
        if (node.Kind == NodeKind.Repeat && node.Control is RepeatControlKind rp && !string.IsNullOrWhiteSpace(rp.IndexVariable))
            yield return rp.IndexVariable.Trim();
    }

    static IEnumerable<string> CollectTemplateStrings(Node node)
    {
        if (node.Action is { } action)
        {
            foreach (var s in ActionTemplateStrings(action))
            {
                if (!string.IsNullOrEmpty(s))
                    yield return s;
            }
        }

        if (node.Control is IfControlKind ic)
        {
            yield return ic.Left;
            yield return ic.Right;
        }
        else if (node.Control is SwitchControlKind sw)
            yield return sw.Expression;
        else if (node.Control is CaseControlKind cc)
            yield return cc.Value;
        else if (node.Control is WhileControlKind wc)
        {
            yield return wc.Left;
            yield return wc.Right;
        }
        else if (node.Control is RepeatControlKind rc)
            yield return rc.Times;
    }

    static IEnumerable<string> ActionTemplateStrings(ActionKind action) => action switch
    {
        LogAction a => [a.Message],
        WebAutomationAction w => Yield(
            w.Url, w.Selector, w.WaitSelectorAfter, w.Text, w.Script, w.Path, w.DropdownValue, w.Detail),
        FileWriteAction a => [a.Path, a.Content],
        FileAppendAction a => [a.Path, a.Content],
        FileReadAction a => [a.Path],
        ListAppendAction a => [a.Item],
        HttpGetAction a => [a.Url],
        HttpRequestAction h => Yield(
            h.Url, h.Accept, h.Body, h.ContentType, h.CustomHeaders),
        LlmCompletionAction l => Yield(
            l.Url, l.Model, l.System, l.User, l.Document, l.JsonSchema, l.Temperature),
        ShowMessageAction a => [a.Title, a.Text],
        ShowGetUserInputAction a => [a.Title, a.Message, a.DefaultValue],
        ShowSelectFromListAction a => [a.Title, a.Message, a.List],
        SetVariableAction a => [a.Value],
        IncreaseVariableAction a => Yield(a.Amount),
        DecreaseVariableAction a => Yield(a.Amount),
        RunFlowAction a => RunFlowStrings(a),
        ExcelAction e => Yield(
            e.Path, e.WorksheetName, e.WorksheetIndex, e.DocumentName, e.StartRow, e.StartColumn, e.EndRow, e.EndColumn,
            e.CellName, e.Value, e.Index, e.Macro, e.NewName, e.FindText, e.ReplaceText, e.ColorHex, e.Mode, e.TableName,
            e.SourceWorksheetName, e.TargetWorksheetName, e.FilterValue1, e.FilterValue2, e.SortColumn2),
        WordAction w => Yield(
            w.Path, w.DocumentName, w.Mode, w.Pages, w.Bookmark, w.Value, w.FindText, w.ReplaceText, w.ImageSource, w.TextToFind),
        PdfAction p => Yield(
            p.Path, p.Mode, p.Page, p.StartPage, p.EndPage, p.Password, p.OutputPath, p.InputPaths, p.ImageFormat, p.RowTolerance),
        VariablesAction v => Yield(
            v.Value, v.Values, v.Items, v.FindText, v.ReplaceText, v.FilterValue, v.FilterValue2, v.FilterConditions, v.JoinColumn, v.JoinColumn2,
            v.Path, v.MinValue, v.MaxValue, v.DecimalPlaces, v.Count, v.ItemIndex, v.JsonText),
        _ => [],
    };

    static IEnumerable<string> RunFlowStrings(RunFlowAction a)
    {
        if (a.FlowId is { } fid)
            yield return fid;
        if (a.Path is { } path)
            yield return path;
        if (a.Inputs is null)
            yield break;
        foreach (var v in a.Inputs.Values)
            yield return v;
    }

    static IEnumerable<string> Yield(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrEmpty(v))
                yield return v;
        }
    }
}
