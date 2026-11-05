using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public bool ShowActionWebAutomation => GetSelectedNode()?.Action is WebAutomationAction;

    WebAutomationAction? WebA() => A<WebAutomationAction>();

    void PatchWeb(Func<WebAutomationActionPatch> patch) =>
        PatchWeb(_ => patch());

    void PatchWeb(Func<WebAutomationAction, WebAutomationActionPatch> patchFields)
    {
        if (WebA() is not { } a)
            return;
        SetAction(WebAutomationActionMutator.With(a, patchFields(a)));
    }

    public string WebUrl
    {
        get => WebA()?.Url ?? "";
        set => PatchWeb(_ => new WebAutomationActionPatch { Url = value });
    }

    public string WebWaitMsText
    {
        get => WebA()?.WaitMs?.ToString() ?? "";
        set => PatchWeb(a => new WebAutomationActionPatch
        {
            WaitMs = string.IsNullOrWhiteSpace(value) ? null : ulong.TryParse(value, out var ms) ? ms : a.WaitMs,
        });
    }

    public bool WebShowOnScreen
    {
        get => WebA()?.ShowOnScreen ?? true;
        set => PatchWeb(_ => new WebAutomationActionPatch { ShowOnScreen = value });
    }

    public bool WebStartMaximized
    {
        get => WebA()?.StartMaximized ?? true;
        set => PatchWeb(_ => new WebAutomationActionPatch { StartMaximized = value });
    }

    public bool ShowWebMaximizeOption => WebA() is { ShowOnScreen: null or true };

    public string WebSelector
    {
        get => WebA()?.Selector ?? "";
        set => PatchWeb(_ => new WebAutomationActionPatch { Selector = value });
    }

    public string WebTimeoutMsText
    {
        get => WebA()?.TimeoutMs?.ToString() ?? "";
        set => PatchWeb(a => new WebAutomationActionPatch
        {
            TimeoutMs = string.IsNullOrWhiteSpace(value) ? null : ulong.TryParse(value, out var ms) ? ms : a.TimeoutMs,
        });
    }

    public string WebText
    {
        get => WebA()?.Text ?? "";
        set => PatchWeb(_ => new WebAutomationActionPatch { Text = value });
    }

    public string WebScript
    {
        get => WebA()?.Script ?? "";
        set => PatchWeb(_ => new WebAutomationActionPatch { Script = value });
    }

    public string WebPath
    {
        get => WebA()?.Path ?? "";
        set => PatchWeb(_ => new WebAutomationActionPatch { Path = value });
    }

    public string WebDetail
    {
        get => WebA()?.Detail ?? "";
        set => PatchWeb(_ => new WebAutomationActionPatch { Detail = value });
    }

    public string WebPopulateMode
    {
        get => WebA()?.PopulateMode ?? WebAutomationEnums.PopulateMode.Replace;
        set => PatchWeb(_ => new WebAutomationActionPatch { PopulateMode = value });
    }

    public string WebWaitContentMode
    {
        get => WebA()?.WaitContentMode ?? WebAutomationEnums.WaitContentMode.ContainsText;
        set => PatchWeb(_ => new WebAutomationActionPatch { WaitContentMode = value });
    }

    public string WebDropdownMode
    {
        get => WebA()?.DropdownMode ?? WebAutomationEnums.DropdownMode.ByText;
        set => PatchWeb(_ => new WebAutomationActionPatch { DropdownMode = value });
    }

    public string WebDropdownValue
    {
        get => WebA()?.DropdownValue ?? "";
        set => PatchWeb(_ => new WebAutomationActionPatch { DropdownValue = value });
    }

    public string WebBrowserEngine
    {
        get => WebA()?.BrowserEngine ?? WebAutomationEnums.BrowserEngine.Chrome;
        set => PatchWeb(_ => new WebAutomationActionPatch { BrowserEngine = value });
    }

    public string WebAttachMode
    {
        get => WebA()?.AttachMode ?? WebAutomationEnums.AttachMode.ByUrl;
        set => PatchWeb(_ => new WebAutomationActionPatch { AttachMode = value });
    }

    public string WebDebuggingPortText
    {
        get => WebA()?.DebuggingPort?.ToString() ?? "";
        set => PatchWeb(a => new WebAutomationActionPatch
        {
            DebuggingPort = string.IsNullOrWhiteSpace(value)
                ? null
                : ulong.TryParse(value, out var p) ? p : a.DebuggingPort,
        });
    }

    public bool WebWaitForNavigation
    {
        get => WebA()?.WaitForNavigation ?? false;
        set => PatchWeb(_ => new WebAutomationActionPatch { WaitForNavigation = value });
    }

    public bool WebWaitVisible
    {
        get => WebA()?.WaitVisible ?? false;
        set => PatchWeb(_ => new WebAutomationActionPatch { WaitVisible = value });
    }

    public bool WebChecked
    {
        get => WebA()?.Checked ?? true;
        set => PatchWeb(_ => new WebAutomationActionPatch { Checked = value });
    }

    public string WebWaitSelectorAfter
    {
        get => WebA()?.WaitSelectorAfter ?? "";
        set => PatchWeb(_ => new WebAutomationActionPatch
        {
            WaitSelectorAfter = string.IsNullOrWhiteSpace(value) ? null : value,
        });
    }

    public string WebWaitAfterTimeoutText
    {
        get => WebA()?.WaitAfterTimeoutMs?.ToString() ?? "";
        set => PatchWeb(a => new WebAutomationActionPatch
        {
            WaitAfterTimeoutMs = string.IsNullOrWhiteSpace(value)
                ? null
                : ulong.TryParse(value, out var ms) ? ms : a.WaitAfterTimeoutMs,
        });
    }

    public string WebVariable
    {
        get => WebA()?.Variable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || WebA() is not { } a)
                return;
            ApplyProducerRenameOr(
                node,
                () => SetAction(WebAutomationActionMutator.With(a, new WebAutomationActionPatch { Variable = value })),
                a.Variable,
                value);
        }
    }

    void RefreshWebAutomationInspector()
    {
        OnPropertyChanged(nameof(ShowActionWebAutomation));
        OnPropertyChanged(nameof(ShowWebMaximizeOption));
        foreach (var name in new[]
        {
            nameof(WebUrl), nameof(WebWaitMsText), nameof(WebShowOnScreen), nameof(WebStartMaximized),
            nameof(WebSelector), nameof(WebTimeoutMsText), nameof(WebText), nameof(WebScript), nameof(WebPath),
            nameof(WebDetail), nameof(WebPopulateMode), nameof(WebWaitContentMode), nameof(WebDropdownMode),
            nameof(WebDropdownValue), nameof(WebBrowserEngine), nameof(WebAttachMode), nameof(WebDebuggingPortText),
            nameof(WebWaitForNavigation), nameof(WebWaitVisible),
            nameof(WebChecked), nameof(WebWaitSelectorAfter), nameof(WebWaitAfterTimeoutText), nameof(WebVariable),
        })
            OnPropertyChanged(name);
    }
}
