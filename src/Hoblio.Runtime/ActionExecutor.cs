using System.Net.Http;
using Hoblio.Model;

namespace Hoblio.Runtime;

public static class ActionExecutor
{
    static readonly HttpClient Http = new();

    public static string Execute(
        ActionKind action,
        BrowserContext browser,
        ExcelContext excel,
        WordContext word,
        RunVariables vars,
        string? projectRoot)
    {
        if (action is RunFlowAction)
            throw new InvalidOperationException("run_flow is executed by the graph runner, not execute_action");

        return action switch
        {
            LogAction log => $"log: {RunVariables.Interpolate(log.Message, vars)}",
            NoOpAction => "noop",
            FileWriteAction fw => FileActions.FileWrite(
                RunVariables.Interpolate(fw.Path, vars),
                RunVariables.Interpolate(fw.Content, vars),
                projectRoot),
            FileAppendAction fa => FileActions.FileAppend(
                RunVariables.Interpolate(fa.Path, vars),
                RunVariables.Interpolate(fa.Content, vars),
                projectRoot),
            FileReadAction fr => FileActions.FileReadIntoVariable(fr.Path, fr.Variable, vars, projectRoot),
            ListAppendAction la => ListActions.ListAppend(vars, la.ListVariable, la.Item),
            ListClearAction lc => ListActions.ListClear(vars, lc.ListVariable),
            DelayAction d => Delay(d.Milliseconds),
            HttpGetAction h => HttpGet(h, vars),
            WebAutomationAction w => browser.Execute(w, vars, projectRoot),
            ShowMessageAction sm => ShowMessage(sm, vars),
            ShowGetUserInputAction ui => ShowGetUserInput(ui, vars),
            ShowSelectFromListAction sl => ShowSelectFromList(sl, vars),
            SetVariableAction sv => SetVariable(sv, vars),
            IncreaseVariableAction or DecreaseVariableAction => AdjustVariable.ExecuteAdjust(action, vars, 1.0),
            ExcelAction ex => excel.Execute(ex, vars, projectRoot),
            WordAction w => word.Execute(w, vars, projectRoot),
            VariablesAction va => VariablesContext.Execute(va, vars, projectRoot),
            _ => throw new InvalidOperationException($"unknown action {action.Type}"),
        };
    }

    public static Dictionary<string, string>? SnapshotVariables(ActionKind action, RunVariables vars)
    {
        var outMap = new Dictionary<string, string>();
        foreach (var name in action.ProducedVariables())
        {
            if (vars.TryGetValue(name, out var value))
                outMap[name] = value;
        }
        return outMap.Count == 0 ? null : outMap;
    }

    static string Delay(ulong ms)
    {
        var remaining = (int)Math.Min(ms, int.MaxValue);
        while (remaining > 0)
        {
            if (RunCancel.IsRequested)
                throw new OperationCanceledException("run cancelled by user");
            var step = Math.Min(remaining, 100);
            Thread.Sleep(step);
            remaining -= step;
        }
        return $"delay: {ms}ms";
    }

    static string HttpGet(HttpGetAction action, RunVariables vars)
    {
        var url = RunVariables.Interpolate(action.Url, vars);
        using var response = Http.GetAsync(url).GetAwaiter().GetResult();
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(action.Variable))
            vars[action.Variable.Trim()] = body;
        var preview = body.Length > 120 ? body[..120] : body;
        var ellipsis = body.Length > 120 ? "…" : "";
        var storeNote = string.IsNullOrWhiteSpace(action.Variable) ? "" : $" → {action.Variable.Trim()}";
        return $"http_get: {url} -> {(int)response.StatusCode} ({body.Length} bytes){storeNote}: {preview}{ellipsis}";
    }

    static string ShowMessage(ShowMessageAction sm, RunVariables vars)
    {
        var title = RunVariables.Interpolate(sm.Title, vars);
        var text = RunVariables.Interpolate(sm.Text, vars);
        NativeDialogs.RunShowMessage(title, text, sm.TimeoutSeconds);
        var timeoutNote = sm.TimeoutSeconds is > 0 and var t ? $"timeout={t}s" : "until closed";
        return $"show_message: \"{title}\" ({timeoutNote})";
    }

    static string ShowGetUserInput(ShowGetUserInputAction ui, RunVariables vars)
    {
        var userVar = ui.UserInputVariable.Trim();
        var buttonVar = ui.ButtonPressedVariable.Trim();
        if (string.IsNullOrEmpty(userVar) || string.IsNullOrEmpty(buttonVar))
            throw new InvalidOperationException("show_get_user_input: output variable names must not be empty");
        var title = RunVariables.Interpolate(ui.Title, vars);
        var message = RunVariables.Interpolate(ui.Message, vars);
        var defaultValue = RunVariables.Interpolate(ui.DefaultValue, vars);
        var result = NativeDialogs.RunShowGetUserInput(title, message, defaultValue, ui.InputKind, ui.Topmost ?? false);
        vars[userVar] = result.UserInput;
        vars[buttonVar] = result.ButtonPressed;
        return $"show_get_user_input: button={result.ButtonPressed} user_input=\"{result.UserInput}\"";
    }

    static string ShowSelectFromList(ShowSelectFromListAction sl, RunVariables vars)
    {
        var itemVar = sl.SelectedItemVariable.Trim();
        var indexVar = sl.SelectedIndexVariable.Trim();
        var buttonVar = sl.ButtonPressedVariable.Trim();
        if (string.IsNullOrEmpty(itemVar) || string.IsNullOrEmpty(indexVar) || string.IsNullOrEmpty(buttonVar))
            throw new InvalidOperationException("show_select_from_list: output variable names must not be empty");

        var title = RunVariables.Interpolate(sl.Title, vars);
        var message = RunVariables.Interpolate(sl.Message, vars);
        var items = SelectFromListItems.Parse(sl.List, vars);
        var result = NativeDialogs.RunShowSelectFromList(
            title,
            message,
            items,
            sl.Topmost ?? false,
            sl.LimitToList ?? true,
            sl.AllowEmptySelection ?? false,
            sl.AllowMultipleSelections ?? false);

        vars[itemVar] = result.SelectedItem;
        vars[indexVar] = result.SelectedIndex;
        vars[buttonVar] = result.ButtonPressed;
        return $"show_select_from_list: button={result.ButtonPressed} item=\"{result.SelectedItem}\" index={result.SelectedIndex}";
    }

    static string SetVariable(SetVariableAction sv, RunVariables vars)
    {
        var name = sv.Variable.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("set_variable: variable name is empty");
        var resolved = RunVariables.Interpolate(sv.Value, vars);
        vars[name] = resolved;
        return $"set_variable: `{name}` = {resolved}";
    }
}
