using Hoblio.Model;

namespace Hoblio.Studio;

public static class ActionDefaults
{
    public static readonly string[] AllActionTypes =
    [
        "log", "no_op", "browser_navigate", "browser_click", "browser_wait_navigation",
        "browser_fill", "browser_wait_for", "browser_get_text", "set_variable",
        "increase_variable", "decrease_variable", "file_write", "file_append", "file_read",
        "list_append", "list_clear", "delay", "http_get", "show_message", "show_get_user_input",
    ];

    public static ActionKind Create(string type, IReadOnlySet<string>? takenNames = null)
    {
        takenNames ??= new HashSet<string>();
        return type switch
        {
            "no_op" => new NoOpAction(),
            "browser_navigate" => new BrowserNavigateAction
            {
                Url = "https://example.com",
                WaitMs = 500,
                ShowOnScreen = true,
                StartMaximized = true,
            },
            "browser_click" => new BrowserClickAction
            {
                Selector = "button",
                TimeoutMs = 10_000,
                WaitForNavigation = false,
            },
            "browser_wait_navigation" => new BrowserWaitNavigationAction { TimeoutMs = 15_000 },
            "browser_fill" => new BrowserFillAction { Selector = "input", Text = "", TimeoutMs = 10_000 },
            "browser_wait_for" => new BrowserWaitForAction { Selector = "h1", TimeoutMs = 10_000 },
            "browser_get_text" => new BrowserGetTextAction
            {
                Selector = "h1",
                TimeoutMs = 10_000,
                Variable = NextName("BrowserText", takenNames),
            },
            "set_variable" => new SetVariableAction
            {
                Variable = NextName("Var", takenNames) ?? "Var1",
                Value = "",
            },
            "increase_variable" => new IncreaseVariableAction { Variable = "counter" },
            "decrease_variable" => new DecreaseVariableAction { Variable = "counter" },
            "file_write" => new FileWriteAction { Path = "output/report.txt", Content = "Hello from Hoblio\n" },
            "file_append" => new FileAppendAction { Path = "output/report.txt", Content = "line\n" },
            "file_read" => new FileReadAction
            {
                Path = "output/report.txt",
                Variable = NextName("FileContent", takenNames) ?? "FileContent1",
            },
            "list_append" => new ListAppendAction { ListVariable = "items", Item = "item" },
            "list_clear" => new ListClearAction { ListVariable = "items" },
            "delay" => new DelayAction { Milliseconds = 1000 },
            "http_get" => new HttpGetAction { Url = "https://example.com" },
            "show_message" => new ShowMessageAction
            {
                Title = "Hoblio",
                Text = "Mensagem de exemplo",
                TimeoutSeconds = 2,
            },
            "show_get_user_input" => new ShowGetUserInputAction
            {
                Title = "Novo",
                Message = "Digite um valor",
                DefaultValue = "",
                UserInputVariable = NextName("UserInput", takenNames) ?? "UserInput",
                ButtonPressedVariable = NextName("ButtonPressed", takenNames) ?? "ButtonPressed",
            },
            _ => new LogAction { Message = "Hello" },
        };
    }

    static string? NextName(string prefix, IReadOnlySet<string> taken)
    {
        for (var i = 1; i <= 99; i++)
        {
            var name = i == 1 ? prefix + "1" : prefix + i;
            if (!taken.Contains(name))
                return name;
        }
        return null;
    }
}
