namespace Hoblio.Model;

public static class WebAutomationActionMutator
{
    public static WebAutomationAction RenameReferences(
        WebAutomationAction a,
        Func<string, string> replaceTemplate,
        Func<string?, string?> renameVar) =>
        new()
        {
            Operation = a.Operation,
            Url = ReplaceOpt(a.Url, replaceTemplate),
            WaitMs = a.WaitMs,
            ShowOnScreen = a.ShowOnScreen,
            StartMaximized = a.StartMaximized,
            Selector = ReplaceOpt(a.Selector, replaceTemplate),
            TimeoutMs = a.TimeoutMs,
            WaitForNavigation = a.WaitForNavigation,
            WaitSelectorAfter = ReplaceOpt(a.WaitSelectorAfter, replaceTemplate),
            WaitAfterTimeoutMs = a.WaitAfterTimeoutMs,
            Text = ReplaceOpt(a.Text, replaceTemplate),
            PopulateMode = a.PopulateMode,
            WaitVisible = a.WaitVisible,
            Variable = renameVar(a.Variable),
            Detail = ReplaceOpt(a.Detail, replaceTemplate),
            Script = ReplaceOpt(a.Script, replaceTemplate),
            Path = ReplaceOpt(a.Path, replaceTemplate),
            Checked = a.Checked,
            DropdownMode = a.DropdownMode,
            DropdownValue = ReplaceOpt(a.DropdownValue, replaceTemplate),
            WaitContentMode = a.WaitContentMode,
            BrowserEngine = a.BrowserEngine,
            AttachMode = a.AttachMode,
            DebuggingPort = a.DebuggingPort,
        };

    static string? ReplaceOpt(string? value, Func<string, string> replace) =>
        value is null ? null : replace(value);

    public static WebAutomationAction With(WebAutomationAction a, WebAutomationActionPatch patch) =>
        new()
        {
            Operation = a.Operation,
            Url = patch.Url ?? a.Url,
            WaitMs = patch.WaitMs ?? a.WaitMs,
            ShowOnScreen = patch.ShowOnScreen ?? a.ShowOnScreen,
            StartMaximized = patch.StartMaximized ?? a.StartMaximized,
            Selector = patch.Selector ?? a.Selector,
            TimeoutMs = patch.TimeoutMs ?? a.TimeoutMs,
            WaitForNavigation = patch.WaitForNavigation ?? a.WaitForNavigation,
            WaitSelectorAfter = patch.WaitSelectorAfter ?? a.WaitSelectorAfter,
            WaitAfterTimeoutMs = patch.WaitAfterTimeoutMs ?? a.WaitAfterTimeoutMs,
            Text = patch.Text ?? a.Text,
            PopulateMode = patch.PopulateMode ?? a.PopulateMode,
            WaitVisible = patch.WaitVisible ?? a.WaitVisible,
            Variable = patch.Variable ?? a.Variable,
            Detail = patch.Detail ?? a.Detail,
            Script = patch.Script ?? a.Script,
            Path = patch.Path ?? a.Path,
            Checked = patch.Checked ?? a.Checked,
            DropdownMode = patch.DropdownMode ?? a.DropdownMode,
            DropdownValue = patch.DropdownValue ?? a.DropdownValue,
            WaitContentMode = patch.WaitContentMode ?? a.WaitContentMode,
            BrowserEngine = patch.BrowserEngine ?? a.BrowserEngine,
            AttachMode = patch.AttachMode ?? a.AttachMode,
            DebuggingPort = patch.DebuggingPort ?? a.DebuggingPort,
        };
}

public sealed class WebAutomationActionPatch
{
    public string? Url { get; init; }
    public ulong? WaitMs { get; init; }
    public bool? ShowOnScreen { get; init; }
    public bool? StartMaximized { get; init; }
    public string? Selector { get; init; }
    public ulong? TimeoutMs { get; init; }
    public bool? WaitForNavigation { get; init; }
    public string? WaitSelectorAfter { get; init; }
    public ulong? WaitAfterTimeoutMs { get; init; }
    public string? Text { get; init; }
    public string? PopulateMode { get; init; }
    public bool? WaitVisible { get; init; }
    public string? Variable { get; init; }
    public string? Detail { get; init; }
    public string? Script { get; init; }
    public string? Path { get; init; }
    public bool? Checked { get; init; }
    public string? DropdownMode { get; init; }
    public string? DropdownValue { get; init; }
    public string? WaitContentMode { get; init; }
    public string? BrowserEngine { get; init; }
    public string? AttachMode { get; init; }
    public ulong? DebuggingPort { get; init; }
}
