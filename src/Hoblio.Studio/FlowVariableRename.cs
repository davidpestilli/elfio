using System.Text.RegularExpressions;
using Hoblio.Model;

namespace Hoblio.Studio;

public static class FlowVariableRename
{
    static readonly Regex BraceRef = new(@"\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.CultureInvariant);

    public static bool WouldDuplicateProducer(IEnumerable<Node> nodes, string newName, string? exceptNodeId)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return false;
        foreach (var p in FlowVariableProducers.Collect(nodes))
        {
            if (exceptNodeId is not null && p.NodeId == exceptNodeId)
                continue;
            if (string.Equals(p.Name, newName, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    public static int ApplyReferences(IList<Node> nodes, string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)
            || string.Equals(oldName, newName, StringComparison.Ordinal))
            return 0;

        var changes = 0;
        foreach (var node in nodes)
        {
            if (node.Action is { } action)
            {
                var next = RenameInAction(action, oldName, newName);
                if (!ReferenceEquals(next, action))
                {
                    node.Action = next;
                    changes++;
                }
            }

            if (node.Control is { } control)
            {
                var next = RenameInControl(control, oldName, newName);
                if (!ReferenceEquals(next, control))
                {
                    node.Control = next;
                    changes++;
                }
            }
        }

        return changes;
    }

    static ActionKind RenameInAction(ActionKind action, string oldName, string newName) => action switch
    {
        LogAction a => new LogAction { Message = ReplaceTemplates(a.Message, oldName, newName) },
        WebAutomationAction w => WebAutomationActionMutator.RenameReferences(
            w,
            s => ReplaceTemplates(s, oldName, newName),
            v => ReplaceVarName(v, oldName, newName)),
        FileWriteAction a => new FileWriteAction
        {
            Path = ReplaceTemplates(a.Path, oldName, newName),
            Content = ReplaceTemplates(a.Content, oldName, newName),
        },
        FileAppendAction a => new FileAppendAction
        {
            Path = ReplaceTemplates(a.Path, oldName, newName),
            Content = ReplaceTemplates(a.Content, oldName, newName),
        },
        FileReadAction a => new FileReadAction
        {
            Path = ReplaceTemplates(a.Path, oldName, newName),
            Variable = ReplaceVarName(a.Variable, oldName, newName),
        },
        ListAppendAction a => new ListAppendAction
        {
            ListVariable = ReplaceVarName(a.ListVariable, oldName, newName),
            Item = ReplaceTemplates(a.Item, oldName, newName),
        },
        ListClearAction a => new ListClearAction { ListVariable = ReplaceVarName(a.ListVariable, oldName, newName) },
        ShowMessageAction a => new ShowMessageAction
        {
            Title = ReplaceTemplates(a.Title, oldName, newName),
            Text = ReplaceTemplates(a.Text, oldName, newName),
            TimeoutSeconds = a.TimeoutSeconds,
        },
        ShowGetUserInputAction a => new ShowGetUserInputAction
        {
            Title = ReplaceTemplates(a.Title, oldName, newName),
            Message = ReplaceTemplates(a.Message, oldName, newName),
            DefaultValue = ReplaceTemplates(a.DefaultValue, oldName, newName),
            InputKind = a.InputKind,
            Topmost = a.Topmost,
            UserInputVariable = ReplaceVarName(a.UserInputVariable, oldName, newName),
            ButtonPressedVariable = ReplaceVarName(a.ButtonPressedVariable, oldName, newName),
        },
        ShowSelectFromListAction a => new ShowSelectFromListAction
        {
            Title = ReplaceTemplates(a.Title, oldName, newName),
            Message = ReplaceTemplates(a.Message, oldName, newName),
            List = ReplaceTemplates(a.List, oldName, newName),
            Topmost = a.Topmost,
            LimitToList = a.LimitToList,
            AllowEmptySelection = a.AllowEmptySelection,
            AllowMultipleSelections = a.AllowMultipleSelections,
            SelectedItemVariable = ReplaceVarName(a.SelectedItemVariable, oldName, newName),
            SelectedIndexVariable = ReplaceVarName(a.SelectedIndexVariable, oldName, newName),
            ButtonPressedVariable = ReplaceVarName(a.ButtonPressedVariable, oldName, newName),
        },
        SetVariableAction a => new SetVariableAction
        {
            Variable = ReplaceVarName(a.Variable, oldName, newName),
            Value = ReplaceTemplates(a.Value, oldName, newName),
        },
        IncreaseVariableAction a => new IncreaseVariableAction
        {
            Variable = ReplaceVarName(a.Variable, oldName, newName),
            Amount = a.Amount is null ? null : ReplaceTemplates(a.Amount, oldName, newName),
        },
        DecreaseVariableAction a => new DecreaseVariableAction
        {
            Variable = ReplaceVarName(a.Variable, oldName, newName),
            Amount = a.Amount is null ? null : ReplaceTemplates(a.Amount, oldName, newName),
        },
        RunFlowAction a => RenameRunFlow(a, oldName, newName),
        HttpGetAction a => new HttpGetAction
        {
            Url = ReplaceTemplates(a.Url, oldName, newName),
            Variable = ReplaceVarName(a.Variable, oldName, newName),
        },
        HttpRequestAction a => new HttpRequestAction
        {
            Url = ReplaceTemplates(a.Url, oldName, newName),
            Method = a.Method,
            Accept = a.Accept is null ? null : ReplaceTemplates(a.Accept, oldName, newName),
            Body = a.Body is null ? null : ReplaceTemplates(a.Body, oldName, newName),
            ContentType = a.ContentType is null ? null : ReplaceTemplates(a.ContentType, oldName, newName),
            CustomHeaders = a.CustomHeaders is null ? null : ReplaceTemplates(a.CustomHeaders, oldName, newName),
            ResponseBodyVariable = ReplaceVarName(a.ResponseBodyVariable, oldName, newName),
            ResponseHeadersVariable = ReplaceVarName(a.ResponseHeadersVariable, oldName, newName),
            StatusCodeVariable = ReplaceVarName(a.StatusCodeVariable, oldName, newName),
        },
        LlmCompletionAction a => new LlmCompletionAction
        {
            Url = ReplaceTemplates(a.Url, oldName, newName),
            ApiStyle = a.ApiStyle,
            Model = ReplaceTemplates(a.Model, oldName, newName),
            System = a.System is null ? null : ReplaceTemplates(a.System, oldName, newName),
            User = a.User is null ? null : ReplaceTemplates(a.User, oldName, newName),
            Document = a.Document is null ? null : ReplaceTemplates(a.Document, oldName, newName),
            JsonSchema = a.JsonSchema is null ? null : ReplaceTemplates(a.JsonSchema, oldName, newName),
            Temperature = a.Temperature is null ? null : ReplaceTemplates(a.Temperature, oldName, newName),
            ResponseBodyVariable = ReplaceVarName(a.ResponseBodyVariable, oldName, newName),
        },
        ExcelAction e => ExcelActionMutator.RenameReferences(
            e,
            text => ReplaceTemplates(text, oldName, newName),
            value => ReplaceVarName(value, oldName, newName)),
        WordAction w => WordActionMutator.RenameReferences(
            w,
            text => ReplaceTemplates(text, oldName, newName),
            value => ReplaceVarName(value, oldName, newName)),
        PdfAction p => PdfActionMutator.RenameReferences(
            p,
            text => ReplaceTemplates(text, oldName, newName),
            value => ReplaceVarName(value, oldName, newName)),
        VariablesAction v => VariablesActionMutator.RenameReferences(
            v,
            text => ReplaceTemplates(text, oldName, newName),
            value => ReplaceVarName(value, oldName, newName)),
        _ => action,
    };

    static RunFlowAction RenameRunFlow(RunFlowAction a, string oldName, string newName)
    {
        Dictionary<string, string>? inputs = null;
        if (a.Inputs is { Count: > 0 })
        {
            inputs = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (k, v) in a.Inputs)
            {
                var key = ReplaceVarName(k, oldName, newName);
                inputs[key] = ReplaceTemplates(v, oldName, newName);
            }
        }

        List<string>? outputs = null;
        if (a.Outputs is { Count: > 0 })
        {
            outputs = a.Outputs.Select(o => ReplaceVarName(o, oldName, newName)).ToList();
        }

        return new RunFlowAction
        {
            Project = a.Project,
            FlowId = a.FlowId is null ? null : ReplaceTemplates(a.FlowId, oldName, newName),
            Path = a.Path is null ? null : ReplaceTemplates(a.Path, oldName, newName),
            Inputs = inputs ?? a.Inputs,
            Outputs = outputs ?? a.Outputs,
            ShareBrowser = a.ShareBrowser,
            VariableScope = a.VariableScope,
        };
    }

    static ControlKind RenameInControl(ControlKind control, string oldName, string newName) => control switch
    {
        IfControlKind c => new IfControlKind
        {
            Left = ReplaceTemplates(c.Left, oldName, newName),
            Right = ReplaceTemplates(c.Right, oldName, newName),
            Operator = c.Operator,
            IgnoreCase = c.IgnoreCase,
        },
        ForEachControlKind c => new ForEachControlKind
        {
            ListVariable = ReplaceVarName(c.ListVariable, oldName, newName),
            ItemVariable = ReplaceVarName(c.ItemVariable, oldName, newName),
        },
        WhileControlKind c => new WhileControlKind
        {
            Left = ReplaceTemplates(c.Left, oldName, newName),
            Right = ReplaceTemplates(c.Right, oldName, newName),
            Operator = c.Operator,
            IgnoreCase = c.IgnoreCase,
        },
        RepeatControlKind c => new RepeatControlKind
        {
            Times = ReplaceTemplates(c.Times, oldName, newName),
            IndexVariable = ReplaceVarName(c.IndexVariable, oldName, newName),
        },
        SwitchControlKind c => new SwitchControlKind
        {
            Expression = ReplaceTemplates(c.Expression, oldName, newName),
            IgnoreCase = c.IgnoreCase,
        },
        CaseControlKind c => new CaseControlKind
        {
            Value = ReplaceTemplates(c.Value, oldName, newName),
            Operator = c.Operator,
        },
        CaseDefaultControlKind => new CaseDefaultControlKind(),
        _ => control,
    };

    static string ReplaceTemplates(string text, string oldName, string newName)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return BraceRef.Replace(text, m =>
            string.Equals(m.Groups[1].Value, oldName, StringComparison.Ordinal)
                ? "{" + newName + "}"
                : m.Value);
    }

    static string ReplaceVarName(string? value, string oldName, string newName) =>
        value is null ? value! : (string.Equals(value.Trim(), oldName, StringComparison.Ordinal) ? newName : value);
}
