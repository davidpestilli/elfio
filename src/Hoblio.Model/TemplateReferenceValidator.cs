using System.Text.RegularExpressions;

namespace Hoblio.Model;

public static class TemplateReferenceValidator
{
    static readonly Regex RefPattern = new(@"\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.CultureInvariant);

    public static void Validate(FlowProject project)
    {
        var known = new HashSet<string>(project.Variables.Keys, StringComparer.Ordinal);
        foreach (var node in project.Flow.Nodes)
        {
            foreach (var name in CollectProducerNames(node))
                known.Add(name);
        }

        foreach (var node in project.Flow.Nodes)
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
                            $"node `{node.Id}` references unknown variable `{{{refName}}}` (not in project variables or flow producers)");
                    }
                }
            }
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
        BrowserNavigateAction a => [a.Url],
        BrowserClickAction a => Yield(a.Selector, a.WaitSelectorAfter),
        BrowserFillAction a => [a.Selector, a.Text],
        BrowserWaitForAction a => [a.Selector],
        BrowserGetTextAction a => [a.Selector],
        FileWriteAction a => [a.Path, a.Content],
        FileAppendAction a => [a.Path, a.Content],
        FileReadAction a => [a.Path],
        ListAppendAction a => [a.Item],
        HttpGetAction a => [a.Url],
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
