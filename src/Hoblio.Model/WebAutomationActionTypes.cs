namespace Hoblio.Model;

/// <summary>Identificadores JSON (<c>type</c>) e telemetria (<c>action_id</c>) — grupo Automação do navegador (PAD WebAutomation).</summary>
public static class WebAutomationActionTypes
{
    // —— já existentes (compatibilidade JSON) ——
    public const string Navigate = "browser_navigate";
    public const string Click = "browser_click";
    public const string WaitNavigation = "browser_wait_navigation";
    public const string Fill = "browser_fill";
    public const string WaitFor = "browser_wait_for";
    public const string GetText = "browser_get_text";

    // —— extração ——
    public const string GetPageDetails = "browser_get_page_details";
    public const string GetElementDetails = "browser_get_element_details";
    public const string Screenshot = "browser_screenshot";
    public const string ExtractTable = "browser_extract_table";

    // —— formulário ——
    public const string Focus = "browser_focus";
    public const string SetCheckbox = "browser_set_checkbox";
    public const string SelectRadio = "browser_select_radio";
    public const string SetDropdown = "browser_set_dropdown";
    public const string PressButton = "browser_press_button";
    public const string Upload = "browser_upload";

    // —— gerais ——
    public const string WaitContent = "browser_wait_content";
    public const string LaunchChrome = "browser_launch_chrome";
    public const string LaunchEdge = "browser_launch_edge";
    public const string AttachChrome = "browser_attach_chrome";
    public const string AttachEdge = "browser_attach_edge";
    public const string NewTab = "browser_new_tab";
    public const string GoTo = "browser_go_to";
    public const string ClickDownload = "browser_click_download";
    public const string ExecuteScript = "browser_execute_script";
    public const string Hover = "browser_hover";
    public const string Close = "browser_close";
    public const string Reload = "browser_reload";
    public const string Back = "browser_back";
    public const string Forward = "browser_forward";
    public const string SwitchPage = "browser_switch_page";

    public static readonly IReadOnlyList<string> All =
    [
        Navigate, Click, WaitNavigation, Fill, WaitFor, GetText,
        GetPageDetails, GetElementDetails, Screenshot, ExtractTable,
        Focus, SetCheckbox, SelectRadio, SetDropdown, PressButton, Upload,
        WaitContent, LaunchChrome, LaunchEdge, AttachChrome, AttachEdge, NewTab, GoTo,
        ClickDownload, ExecuteScript, Hover, Close, Reload, Back, Forward, SwitchPage,
    ];

    public static readonly IReadOnlyDictionary<string, string> ModuleActionIds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Navigate] = "LaunchChromeBase",
            [Click] = "ClickBase",
            [WaitNavigation] = "WaitForPageToLoadBase",
            [Fill] = "PopulateTextFieldBase",
            [WaitFor] = "WaitForElementBase",
            [GetText] = "GetDetailsOfElementBase",
            [GetPageDetails] = "GetDetailsOfWebPage",
            [GetElementDetails] = "GetDetailsOfElement",
            [Screenshot] = "TakeScreenshotBase",
            [ExtractTable] = "ExtractData",
            [Focus] = "FocusBase",
            [SetCheckbox] = "SetCheckboxStateBase",
            [SelectRadio] = "SelectRadioButtonBase",
            [SetDropdown] = "SetDropDownListValueBase",
            [PressButton] = "PressButtonBase",
            [Upload] = "UploadFile",
            [WaitContent] = "WaitForWebPageContentAction",
            [LaunchChrome] = "LaunchChromeBase",
            [LaunchEdge] = "LaunchEdgeBase",
            [AttachChrome] = "AttachToChrome",
            [AttachEdge] = "AttachToEdge",
            [NewTab] = "CreateNewTabBase",
            [GoTo] = "GoToWebPageBase",
            [ClickDownload] = "ClickDownloadLink",
            [ExecuteScript] = "ExecuteJavascript",
            [Hover] = "HoverOverElement",
            [Close] = "CloseWebBrowser",
            [Reload] = "ReloadWebPage",
            [Back] = "NavigateBack",
            [Forward] = "NavigateForward",
            [SwitchPage] = "SwitchToPage",
        };

    public static bool IsWebAutomationType(string? type) =>
        type is not null && ModuleActionIds.ContainsKey(type);

    public static string ModuleActionId(string type) =>
        ModuleActionIds.TryGetValue(type, out var id) ? id : type;

    /// <summary>Telemetria PAD (AttachToChromeByUrl, AttachToForegroundEdge, …).</summary>
    public static string TelemetryActionId(WebAutomationAction a)
    {
        var mode = a.AttachMode ?? WebAutomationEnums.AttachMode.ByUrl;
        return a.Type switch
        {
            AttachChrome => mode switch
            {
                WebAutomationEnums.AttachMode.ByTitle => "AttachToChromeByTitle",
                WebAutomationEnums.AttachMode.Foreground => "AttachToForegroundChrome",
                _ => "AttachToChromeByUrl",
            },
            AttachEdge => mode switch
            {
                WebAutomationEnums.AttachMode.ByTitle => "AttachToEdgeByTitle",
                WebAutomationEnums.AttachMode.Foreground => "AttachToTheForegroundEdge",
                _ => "AttachToEdgeByUrl",
            },
            _ => ModuleActionId(a.Type),
        };
    }
}
