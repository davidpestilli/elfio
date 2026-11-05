using Hoblio.Model;

namespace Hoblio.Studio;

static class WebAutomationActionDefaults
{
    public static WebAutomationAction? TryCreate(string type, IReadOnlySet<string>? takenNames)
    {
        if (!WebAutomationActionTypes.IsWebAutomationType(type))
            return null;

        var textVar = NextName("BrowserText", takenNames) ?? "BrowserText1";

        return type switch
        {
            WebAutomationActionTypes.Navigate => new WebAutomationAction
            {
                Operation = type,
                Url = "https://example.com",
                WaitMs = 500,
                ShowOnScreen = true,
                StartMaximized = true,
            },
            WebAutomationActionTypes.GoTo => new WebAutomationAction
            {
                Operation = type,
                Url = "https://example.com",
            },
            WebAutomationActionTypes.LaunchChrome => new WebAutomationAction
            {
                Operation = type,
                BrowserEngine = WebAutomationEnums.BrowserEngine.Chrome,
                ShowOnScreen = true,
                StartMaximized = true,
            },
            WebAutomationActionTypes.LaunchEdge => new WebAutomationAction
            {
                Operation = type,
                BrowserEngine = WebAutomationEnums.BrowserEngine.Edge,
                ShowOnScreen = true,
                StartMaximized = true,
            },
            WebAutomationActionTypes.AttachChrome => new WebAutomationAction
            {
                Operation = type,
                AttachMode = WebAutomationEnums.AttachMode.Foreground,
                TimeoutMs = 10_000,
            },
            WebAutomationActionTypes.AttachEdge => new WebAutomationAction
            {
                Operation = type,
                AttachMode = WebAutomationEnums.AttachMode.Foreground,
                TimeoutMs = 10_000,
            },
            WebAutomationActionTypes.Click => new WebAutomationAction
            {
                Operation = type,
                Selector = "button",
                TimeoutMs = 10_000,
                WaitForNavigation = false,
            },
            WebAutomationActionTypes.ClickDownload => new WebAutomationAction
            {
                Operation = type,
                Selector = "a[download]",
                Path = "output/download",
                TimeoutMs = 30_000,
            },
            WebAutomationActionTypes.WaitNavigation => new WebAutomationAction
            {
                Operation = type,
                TimeoutMs = 15_000,
            },
            WebAutomationActionTypes.Fill => new WebAutomationAction
            {
                Operation = type,
                Selector = "input",
                Text = "",
                TimeoutMs = 10_000,
                PopulateMode = WebAutomationEnums.PopulateMode.Replace,
            },
            WebAutomationActionTypes.Focus => new WebAutomationAction
            {
                Operation = type,
                Selector = "input",
                TimeoutMs = 10_000,
            },
            WebAutomationActionTypes.WaitFor => new WebAutomationAction
            {
                Operation = type,
                Selector = "h1",
                TimeoutMs = 10_000,
            },
            WebAutomationActionTypes.WaitContent => new WebAutomationAction
            {
                Operation = type,
                WaitContentMode = WebAutomationEnums.WaitContentMode.ContainsText,
                Text = "Example",
                TimeoutMs = 10_000,
            },
            WebAutomationActionTypes.GetText => new WebAutomationAction
            {
                Operation = type,
                Selector = "h1",
                TimeoutMs = 10_000,
                Variable = textVar,
            },
            WebAutomationActionTypes.GetPageDetails => new WebAutomationAction
            {
                Operation = type,
                Detail = WebAutomationEnums.PageDetail.Title,
                Variable = textVar,
            },
            WebAutomationActionTypes.GetElementDetails => new WebAutomationAction
            {
                Operation = type,
                Selector = "h1",
                Detail = WebAutomationEnums.ElementDetail.Text,
                TimeoutMs = 10_000,
                Variable = textVar,
            },
            WebAutomationActionTypes.Screenshot => new WebAutomationAction
            {
                Operation = type,
                Path = "output/web-screenshot.png",
            },
            WebAutomationActionTypes.ExtractTable => new WebAutomationAction
            {
                Operation = type,
                Selector = "table",
                TimeoutMs = 10_000,
                Variable = NextName("BrowserTable", takenNames) ?? "BrowserTable1",
            },
            WebAutomationActionTypes.SetCheckbox => new WebAutomationAction
            {
                Operation = type,
                Selector = "input[type=checkbox]",
                Checked = true,
                TimeoutMs = 10_000,
            },
            WebAutomationActionTypes.SelectRadio => new WebAutomationAction
            {
                Operation = type,
                Selector = "input[type=radio]",
                TimeoutMs = 10_000,
            },
            WebAutomationActionTypes.SetDropdown => new WebAutomationAction
            {
                Operation = type,
                Selector = "select",
                DropdownMode = WebAutomationEnums.DropdownMode.ByText,
                DropdownValue = "",
                TimeoutMs = 10_000,
            },
            WebAutomationActionTypes.PressButton => new WebAutomationAction
            {
                Operation = type,
                Selector = "button[type=submit]",
                TimeoutMs = 10_000,
            },
            WebAutomationActionTypes.Hover => new WebAutomationAction
            {
                Operation = type,
                Selector = "a",
                TimeoutMs = 10_000,
            },
            WebAutomationActionTypes.ExecuteScript => new WebAutomationAction
            {
                Operation = type,
                Script = "document.title",
                Variable = textVar,
            },
            WebAutomationActionTypes.NewTab => new WebAutomationAction
            {
                Operation = type,
                Url = "about:blank",
            },
            WebAutomationActionTypes.Close or WebAutomationActionTypes.Reload
                or WebAutomationActionTypes.Back or WebAutomationActionTypes.Forward => new WebAutomationAction
            {
                Operation = type,
                TimeoutMs = 15_000,
            },
            _ => new WebAutomationAction { Operation = type },
        };
    }

    static string? NextName(string prefix, IReadOnlySet<string>? taken)
    {
        if (taken is null || taken.Count == 0)
            return null;
        for (var i = 1; i <= 99; i++)
        {
            var name = i == 1 ? prefix + "1" : prefix + i;
            if (!taken.Contains(name))
                return name;
        }
        return null;
    }
}
