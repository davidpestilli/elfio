using System.Net.Http;
using Hoblio.Model;

namespace Hoblio.Runtime;

public static class ActionExecutor
{
    static readonly HttpClient Http = new();

    public static string Execute(
        ActionKind action,
        BrowserContext browser,
        RunVariables vars,
        string? projectRoot)
    {
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
            HttpGetAction h => HttpGet(h.Url),
            BrowserNavigateAction n => browser.Navigate(n.Url, n.WaitMs, n.ShowOnScreen, n.StartMaximized),
            BrowserClickAction c => browser.Click(c.Selector, c.TimeoutMs, c.WaitForNavigation ?? false, c.WaitSelectorAfter, c.WaitAfterTimeoutMs),
            BrowserWaitNavigationAction w => browser.WaitNavigation(w.TimeoutMs),
            BrowserFillAction f => browser.Fill(
                RunVariables.Interpolate(f.Selector, vars),
                RunVariables.Interpolate(f.Text, vars),
                f.TimeoutMs),
            BrowserWaitForAction wf => browser.WaitFor(wf.Selector, wf.TimeoutMs),
            BrowserGetTextAction gt => browser.GetText(gt.Selector, gt.TimeoutMs, gt.Variable, vars),
            ShowMessageAction sm => ShowMessage(sm, vars),
            ShowGetUserInputAction ui => ShowGetUserInput(ui, vars),
            SetVariableAction sv => SetVariable(sv, vars),
            IncreaseVariableAction or DecreaseVariableAction => AdjustVariable.ExecuteAdjust(action, vars, 1.0),
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
        Thread.Sleep((int)ms);
        return $"delay: {ms}ms";
    }

    static string HttpGet(string url)
    {
        using var response = Http.GetAsync(url).GetAwaiter().GetResult();
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var preview = body.Length > 120 ? body[..120] : body;
        var ellipsis = body.Length > 120 ? "…" : "";
        return $"http_get: {url} -> {(int)response.StatusCode} ({body.Length} bytes): {preview}{ellipsis}";
    }

    static string ShowMessage(ShowMessageAction sm, RunVariables vars)
    {
        var title = RunVariables.Interpolate(sm.Title, vars);
        var text = RunVariables.Interpolate(sm.Text, vars);
        NativeDialogs.SpawnShowMessage(title, text, sm.TimeoutSeconds);
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
