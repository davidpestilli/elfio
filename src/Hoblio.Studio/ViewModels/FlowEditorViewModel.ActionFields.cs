using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public string CurrentActionSummary
    {
        get
        {
            var node = GetSelectedNode();
            if (node?.Action is null)
                return "";
            return node.Action switch
            {
                LogAction a => $"Log: {Truncate(a.Message, 40)}",
                BrowserNavigateAction a => $"Navigate: {Truncate(a.Url, 40)}",
                BrowserClickAction a => $"Click: {Truncate(a.Selector, 40)}",
                BrowserFillAction a => $"Fill: {Truncate(a.Selector, 40)}",
                BrowserGetTextAction a => $"GetText → {a.Variable ?? "?"}",
                RunFlowAction a => $"RunFlow: {a.FlowId ?? a.Path ?? "?"}",
                SetVariableAction a => $"Set {a.Variable}",
                ShowGetUserInputAction a => $"Input: {Truncate(a.Title, 30)}",
                _ => node.Action.Type,
            };
        }
    }

    public string ActionTemplateHint
    {
        get
        {
            var names = ProjectScalars.Select(s => s.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Take(3).ToList();
            return names.Count == 0 ? "" : $"Use {{name}} — e.g. {string.Join(", ", names.Select(n => $"{{{n}}}"))}";
        }
    }

    public bool ShowActionTemplateHint => !string.IsNullOrEmpty(ActionTemplateHint);
    public bool ShowActionNoOp => GetSelectedNode()?.Action is NoOpAction;
    public bool ShowActionFileWrite => GetSelectedNode()?.Action is FileWriteAction;
    public bool ShowActionFileAppend => GetSelectedNode()?.Action is FileAppendAction;
    public bool ShowActionFileRead => GetSelectedNode()?.Action is FileReadAction;
    public bool ShowActionListClear => GetSelectedNode()?.Action is ListClearAction;
    public bool ShowActionListAppend => GetSelectedNode()?.Action is ListAppendAction;
    public bool ShowActionHttpGet => GetSelectedNode()?.Action is HttpGetAction;
    public bool ShowActionShowMessage => GetSelectedNode()?.Action is ShowMessageAction;
    public bool ShowActionShowGetUserInput => GetSelectedNode()?.Action is ShowGetUserInputAction;
    public bool ShowActionBrowserNavigate => GetSelectedNode()?.Action is BrowserNavigateAction;
    public bool ShowActionBrowserWaitNav => GetSelectedNode()?.Action is BrowserWaitNavigationAction;
    public bool ShowActionDelay => GetSelectedNode()?.Action is DelayAction;
    public bool ShowActionBrowserDom => GetSelectedNode()?.Action is BrowserClickAction or BrowserFillAction or BrowserWaitForAction or BrowserGetTextAction;
    public bool ShowActionBrowserClick => GetSelectedNode()?.Action is BrowserClickAction;
    public bool ShowActionBrowserFillExtra => GetSelectedNode()?.Action is BrowserFillAction;
    public bool ShowActionBrowserGetTextExtra => GetSelectedNode()?.Action is BrowserGetTextAction;
    public bool ShowActionAdjustVariable => GetSelectedNode()?.Action is IncreaseVariableAction or DecreaseVariableAction;
    public bool ShowActionSetVariable => GetSelectedNode()?.Action is SetVariableAction;
    public bool ShowBrowserNavigateMaximizeOption =>
        GetSelectedNode()?.Action is BrowserNavigateAction n && (n.ShowOnScreen ?? true);

    public string FileWritePath { get => A<FileWriteAction>()?.Path ?? ""; set => SetAction(A<FileWriteAction>() is { } a ? new FileWriteAction { Path = value, Content = a.Content } : null); }
    public string FileWriteContent { get => A<FileWriteAction>()?.Content ?? ""; set => SetAction(A<FileWriteAction>() is { } a ? new FileWriteAction { Path = a.Path, Content = value } : null); }
    public string FileAppendPath { get => A<FileAppendAction>()?.Path ?? ""; set => SetAction(A<FileAppendAction>() is { } a ? new FileAppendAction { Path = value, Content = a.Content } : null); }
    public string FileAppendContent { get => A<FileAppendAction>()?.Content ?? ""; set => SetAction(A<FileAppendAction>() is { } a ? new FileAppendAction { Path = a.Path, Content = value } : null); }
    public string FileReadPath { get => A<FileReadAction>()?.Path ?? ""; set => SetAction(A<FileReadAction>() is { } a ? new FileReadAction { Path = value, Variable = a.Variable } : null); }
    public string FileReadVariable
    {
        get => A<FileReadAction>()?.Variable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<FileReadAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node, () => node.Action = new FileReadAction { Path = a.Path, Variable = value }, a.Variable, value);
        }
    }

    public string ListClearVariable { get => A<ListClearAction>()?.ListVariable ?? ""; set => SetAction(new ListClearAction { ListVariable = value }); }
    public string ListAppendVariable { get => A<ListAppendAction>()?.ListVariable ?? ""; set => SetAction(A<ListAppendAction>() is { } a ? new ListAppendAction { ListVariable = value, Item = a.Item } : null); }
    public string ListAppendItem { get => A<ListAppendAction>()?.Item ?? ""; set => SetAction(A<ListAppendAction>() is { } a ? new ListAppendAction { ListVariable = a.ListVariable, Item = value } : null); }

    public string HttpGetUrl
    {
        get => A<HttpGetAction>()?.Url ?? "";
        set => SetAction(A<HttpGetAction>() is { } a ? new HttpGetAction { Url = value, Variable = a.Variable } : new HttpGetAction { Url = value });
    }

    public string HttpGetVariable
    {
        get => A<HttpGetAction>()?.Variable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<HttpGetAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node, () => node.Action = new HttpGetAction { Url = a.Url, Variable = value }, a.Variable, value);
        }
    }

    public string ShowMessageTitle { get => A<ShowMessageAction>()?.Title ?? ""; set => SetAction(A<ShowMessageAction>() is { } a ? new ShowMessageAction { Title = value, Text = a.Text, TimeoutSeconds = a.TimeoutSeconds } : null); }
    public string ShowMessageText { get => A<ShowMessageAction>()?.Text ?? ""; set => SetAction(A<ShowMessageAction>() is { } a ? new ShowMessageAction { Title = a.Title, Text = value, TimeoutSeconds = a.TimeoutSeconds } : null); }
    public string ShowMessageTimeoutText
    {
        get => A<ShowMessageAction>()?.TimeoutSeconds?.ToString() ?? "";
        set => SetAction(A<ShowMessageAction>() is { } a ? new ShowMessageAction
        {
            Title = a.Title,
            Text = a.Text,
            TimeoutSeconds = string.IsNullOrWhiteSpace(value) ? null : double.TryParse(value, out var d) ? d : a.TimeoutSeconds,
        } : null);
    }

    public string UserInputTitle { get => A<ShowGetUserInputAction>()?.Title ?? ""; set => PatchUserInput(a => new ShowGetUserInputAction { Title = value, Message = a.Message, DefaultValue = a.DefaultValue, InputKind = a.InputKind, Topmost = a.Topmost, UserInputVariable = a.UserInputVariable, ButtonPressedVariable = a.ButtonPressedVariable }); }
    public string UserInputMessage { get => A<ShowGetUserInputAction>()?.Message ?? ""; set => PatchUserInput(a => new ShowGetUserInputAction { Title = a.Title, Message = value, DefaultValue = a.DefaultValue, InputKind = a.InputKind, Topmost = a.Topmost, UserInputVariable = a.UserInputVariable, ButtonPressedVariable = a.ButtonPressedVariable }); }
    public string UserInputDefault { get => A<ShowGetUserInputAction>()?.DefaultValue ?? ""; set => PatchUserInput(a => new ShowGetUserInputAction { Title = a.Title, Message = a.Message, DefaultValue = value, InputKind = a.InputKind, Topmost = a.Topmost, UserInputVariable = a.UserInputVariable, ButtonPressedVariable = a.ButtonPressedVariable }); }
    public string UserInputKindMode
    {
        get => A<ShowGetUserInputAction>()?.InputKind == UserInputKind.MultiLine ? "multi_line" : "single_line";
        set => PatchUserInput(a => new ShowGetUserInputAction
        {
            Title = a.Title,
            Message = a.Message,
            DefaultValue = a.DefaultValue,
            InputKind = value == "multi_line" ? UserInputKind.MultiLine : UserInputKind.SingleLine,
            Topmost = a.Topmost,
            UserInputVariable = a.UserInputVariable,
            ButtonPressedVariable = a.ButtonPressedVariable,
        });
    }
    public bool UserInputTopmost { get => A<ShowGetUserInputAction>()?.Topmost ?? false; set => PatchUserInput(a => new ShowGetUserInputAction { Title = a.Title, Message = a.Message, DefaultValue = a.DefaultValue, InputKind = a.InputKind, Topmost = value, UserInputVariable = a.UserInputVariable, ButtonPressedVariable = a.ButtonPressedVariable }); }
    public string UserInputVariable
    {
        get => A<ShowGetUserInputAction>()?.UserInputVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<ShowGetUserInputAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node, () => node.Action = new ShowGetUserInputAction
            {
                Title = a.Title, Message = a.Message, DefaultValue = a.DefaultValue, InputKind = a.InputKind, Topmost = a.Topmost,
                UserInputVariable = value, ButtonPressedVariable = a.ButtonPressedVariable,
            }, a.UserInputVariable, value);
        }
    }

    public string ButtonPressedVariable
    {
        get => A<ShowGetUserInputAction>()?.ButtonPressedVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<ShowGetUserInputAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node, () => node.Action = new ShowGetUserInputAction
            {
                Title = a.Title, Message = a.Message, DefaultValue = a.DefaultValue, InputKind = a.InputKind, Topmost = a.Topmost,
                UserInputVariable = a.UserInputVariable, ButtonPressedVariable = value,
            }, a.ButtonPressedVariable, value);
        }
    }

    public string BrowserNavigateUrl { get => A<BrowserNavigateAction>()?.Url ?? ""; set => PatchNavigate(a => new BrowserNavigateAction { Url = value, WaitMs = a.WaitMs, ShowOnScreen = a.ShowOnScreen, StartMaximized = a.StartMaximized }); }
    public string BrowserNavigateWaitMsText
    {
        get => A<BrowserNavigateAction>()?.WaitMs?.ToString() ?? "";
        set => PatchNavigate(a => new BrowserNavigateAction
        {
            Url = a.Url,
            WaitMs = string.IsNullOrWhiteSpace(value) ? null : ulong.TryParse(value, out var ms) ? ms : a.WaitMs,
            ShowOnScreen = a.ShowOnScreen,
            StartMaximized = a.StartMaximized,
        });
    }
    public bool BrowserNavigateShowOnScreen { get => A<BrowserNavigateAction>()?.ShowOnScreen ?? true; set => PatchNavigate(a => new BrowserNavigateAction { Url = a.Url, WaitMs = a.WaitMs, ShowOnScreen = value, StartMaximized = a.StartMaximized }); }
    public bool BrowserNavigateStartMaximized { get => A<BrowserNavigateAction>()?.StartMaximized ?? true; set => PatchNavigate(a => new BrowserNavigateAction { Url = a.Url, WaitMs = a.WaitMs, ShowOnScreen = a.ShowOnScreen, StartMaximized = value }); }

    public string BrowserWaitNavTimeoutText
    {
        get => A<BrowserWaitNavigationAction>()?.TimeoutMs?.ToString() ?? "";
        set => SetAction(new BrowserWaitNavigationAction
        {
            TimeoutMs = string.IsNullOrWhiteSpace(value) ? null : ulong.TryParse(value, out var ms) ? ms : A<BrowserWaitNavigationAction>()?.TimeoutMs,
        });
    }

    public string DelayMillisecondsText
    {
        get => A<DelayAction>()?.Milliseconds.ToString() ?? "0";
        set => SetAction(new DelayAction { Milliseconds = ulong.TryParse(value, out var ms) ? ms : 0 });
    }

    public string BrowserSelector
    {
        get => GetSelectedNode()?.Action switch
        {
            BrowserClickAction a => a.Selector,
            BrowserFillAction a => a.Selector,
            BrowserWaitForAction a => a.Selector,
            BrowserGetTextAction a => a.Selector,
            _ => "",
        };
        set => SetAction(GetSelectedNode()?.Action switch
        {
            BrowserClickAction a => new BrowserClickAction { Selector = value, TimeoutMs = a.TimeoutMs, WaitForNavigation = a.WaitForNavigation, WaitSelectorAfter = a.WaitSelectorAfter, WaitAfterTimeoutMs = a.WaitAfterTimeoutMs },
            BrowserFillAction a => new BrowserFillAction { Selector = value, Text = a.Text, TimeoutMs = a.TimeoutMs },
            BrowserWaitForAction a => new BrowserWaitForAction { Selector = value, TimeoutMs = a.TimeoutMs },
            BrowserGetTextAction a => new BrowserGetTextAction { Selector = value, TimeoutMs = a.TimeoutMs, Variable = a.Variable },
            _ => null,
        });
    }

    public string BrowserTimeoutMsText
    {
        get => GetSelectedNode()?.Action switch
        {
            BrowserClickAction a => a.TimeoutMs?.ToString() ?? "",
            BrowserFillAction a => a.TimeoutMs?.ToString() ?? "",
            BrowserWaitForAction a => a.TimeoutMs?.ToString() ?? "",
            BrowserGetTextAction a => a.TimeoutMs?.ToString() ?? "",
            _ => "",
        };
        set
        {
            ulong? ms = string.IsNullOrWhiteSpace(value) ? null : ulong.TryParse(value, out var n) ? n : null;
            SetAction(GetSelectedNode()?.Action switch
            {
                BrowserClickAction a => new BrowserClickAction { Selector = a.Selector, TimeoutMs = ms, WaitForNavigation = a.WaitForNavigation, WaitSelectorAfter = a.WaitSelectorAfter, WaitAfterTimeoutMs = a.WaitAfterTimeoutMs },
                BrowserFillAction a => new BrowserFillAction { Selector = a.Selector, Text = a.Text, TimeoutMs = ms },
                BrowserWaitForAction a => new BrowserWaitForAction { Selector = a.Selector, TimeoutMs = ms },
                BrowserGetTextAction a => new BrowserGetTextAction { Selector = a.Selector, TimeoutMs = ms, Variable = a.Variable },
                _ => null,
            });
        }
    }

    public bool BrowserClickWaitForNavigation { get => A<BrowserClickAction>()?.WaitForNavigation ?? false; set => PatchClick(a => new BrowserClickAction { Selector = a.Selector, TimeoutMs = a.TimeoutMs, WaitForNavigation = value, WaitSelectorAfter = a.WaitSelectorAfter, WaitAfterTimeoutMs = a.WaitAfterTimeoutMs }); }
    public string BrowserClickWaitSelectorAfter { get => A<BrowserClickAction>()?.WaitSelectorAfter ?? ""; set => PatchClick(a => new BrowserClickAction { Selector = a.Selector, TimeoutMs = a.TimeoutMs, WaitForNavigation = a.WaitForNavigation, WaitSelectorAfter = string.IsNullOrWhiteSpace(value) ? null : value, WaitAfterTimeoutMs = a.WaitAfterTimeoutMs }); }
    public string BrowserClickWaitAfterTimeoutText
    {
        get => A<BrowserClickAction>()?.WaitAfterTimeoutMs?.ToString() ?? "";
        set => PatchClick(a => new BrowserClickAction
        {
            Selector = a.Selector,
            TimeoutMs = a.TimeoutMs,
            WaitForNavigation = a.WaitForNavigation,
            WaitSelectorAfter = a.WaitSelectorAfter,
            WaitAfterTimeoutMs = string.IsNullOrWhiteSpace(value) ? null : ulong.TryParse(value, out var ms) ? ms : a.WaitAfterTimeoutMs,
        });
    }

    public string BrowserFillText { get => A<BrowserFillAction>()?.Text ?? ""; set => SetAction(A<BrowserFillAction>() is { } a ? new BrowserFillAction { Selector = a.Selector, Text = value, TimeoutMs = a.TimeoutMs } : null); }
    public string BrowserGetTextVariable
    {
        get => A<BrowserGetTextAction>()?.Variable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<BrowserGetTextAction>() is not { } a)
                return;
            var trimmed = string.IsNullOrWhiteSpace(value) ? null : value;
            ApplyProducerRenameOr(node, () => node.Action = new BrowserGetTextAction
            {
                Selector = a.Selector, TimeoutMs = a.TimeoutMs, Variable = trimmed,
            }, a.Variable, trimmed ?? "", allowEmptyProducer: true);
        }
    }

    public string SetVariableName
    {
        get => A<SetVariableAction>()?.Variable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<SetVariableAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node, () => node.Action = new SetVariableAction { Variable = value, Value = a.Value }, a.Variable, value);
        }
    }
    public string SetVariableValue { get => A<SetVariableAction>()?.Value ?? ""; set => SetAction(A<SetVariableAction>() is { } a ? new SetVariableAction { Variable = a.Variable, Value = value } : null); }

    public string AdjustVariableName
    {
        get => GetSelectedNode()?.Action switch { IncreaseVariableAction a => a.Variable, DecreaseVariableAction a => a.Variable, _ => "" };
        set => SetAction(GetSelectedNode()?.Action switch
        {
            IncreaseVariableAction a => new IncreaseVariableAction { Variable = value, Amount = a.Amount },
            DecreaseVariableAction a => new DecreaseVariableAction { Variable = value, Amount = a.Amount },
            _ => null,
        });
    }

    public string AdjustVariableAmountText
    {
        get => GetSelectedNode()?.Action switch { IncreaseVariableAction a => a.Amount ?? "", DecreaseVariableAction a => a.Amount ?? "", _ => "" };
        set
        {
            var amt = string.IsNullOrWhiteSpace(value) ? null : value;
            SetAction(GetSelectedNode()?.Action switch
            {
                IncreaseVariableAction a => new IncreaseVariableAction { Variable = a.Variable, Amount = amt },
                DecreaseVariableAction a => new DecreaseVariableAction { Variable = a.Variable, Amount = amt },
                _ => null,
            });
        }
    }

    T? A<T>() where T : ActionKind => GetSelectedNode()?.Action as T;

    void PatchNavigate(Func<BrowserNavigateAction, BrowserNavigateAction> map)
    {
        if (A<BrowserNavigateAction>() is not { } a)
            return;
        SetAction(map(a));
    }

    void PatchClick(Func<BrowserClickAction, BrowserClickAction> map)
    {
        if (A<BrowserClickAction>() is not { } a)
            return;
        SetAction(map(a));
    }

    void PatchUserInput(Func<ShowGetUserInputAction, ShowGetUserInputAction> map)
    {
        if (A<ShowGetUserInputAction>() is not { } a)
            return;
        SetAction(map(a));
    }

    void SetAction(ActionKind? next)
    {
        if (next is null)
            return;
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.Action)
            return;
        node.Action = next;
        CommitActionEdit(node);
    }

    void CommitActionEdit(Node node)
    {
        ProducerRenameError = null;
        RefreshNodeSummary(node);
        MarkDirty();
        RefreshActionFieldBindings();
        ReloadFlowVariableProducers();
        RebuildTemplateVariableNames();
    }

    static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    void RefreshActionFieldBindings()
    {
        OnPropertyChanged(nameof(CurrentActionSummary));
        OnPropertyChanged(nameof(ActionTemplateHint));
        OnPropertyChanged(nameof(ShowActionTemplateHint));
        OnPropertyChanged(nameof(ShowActionNoOp));
        OnPropertyChanged(nameof(ShowActionFileWrite));
        OnPropertyChanged(nameof(ShowActionFileAppend));
        OnPropertyChanged(nameof(ShowActionFileRead));
        OnPropertyChanged(nameof(ShowActionListClear));
        OnPropertyChanged(nameof(ShowActionListAppend));
        OnPropertyChanged(nameof(ShowActionHttpGet));
        OnPropertyChanged(nameof(ShowActionShowMessage));
        OnPropertyChanged(nameof(ShowActionShowGetUserInput));
        OnPropertyChanged(nameof(ShowActionBrowserNavigate));
        OnPropertyChanged(nameof(ShowActionBrowserWaitNav));
        OnPropertyChanged(nameof(ShowActionDelay));
        OnPropertyChanged(nameof(ShowActionBrowserDom));
        OnPropertyChanged(nameof(ShowActionBrowserClick));
        OnPropertyChanged(nameof(ShowActionBrowserFillExtra));
        OnPropertyChanged(nameof(ShowActionBrowserGetTextExtra));
        OnPropertyChanged(nameof(ShowActionAdjustVariable));
        OnPropertyChanged(nameof(ShowActionSetVariable));
        OnPropertyChanged(nameof(ShowBrowserNavigateMaximizeOption));
        NotifyAllActionFieldProperties();
    }

    void NotifyAllActionFieldProperties()
    {
        foreach (var name in new[]
        {
            nameof(FileWritePath), nameof(FileWriteContent), nameof(FileAppendPath), nameof(FileAppendContent),
            nameof(FileReadPath), nameof(FileReadVariable), nameof(ListClearVariable), nameof(ListAppendVariable),
            nameof(ListAppendItem), nameof(HttpGetUrl), nameof(HttpGetVariable), nameof(ShowMessageTitle), nameof(ShowMessageText),
            nameof(ShowMessageTimeoutText), nameof(UserInputTitle), nameof(UserInputMessage), nameof(UserInputDefault),
            nameof(UserInputKindMode), nameof(UserInputTopmost), nameof(UserInputVariable), nameof(ButtonPressedVariable),
            nameof(BrowserNavigateUrl), nameof(BrowserNavigateWaitMsText), nameof(BrowserNavigateShowOnScreen),
            nameof(BrowserNavigateStartMaximized), nameof(BrowserWaitNavTimeoutText), nameof(DelayMillisecondsText),
            nameof(BrowserSelector), nameof(BrowserTimeoutMsText), nameof(BrowserClickWaitForNavigation),
            nameof(BrowserClickWaitSelectorAfter), nameof(BrowserClickWaitAfterTimeoutText), nameof(BrowserFillText),
            nameof(BrowserGetTextVariable), nameof(SetVariableName), nameof(SetVariableValue),
            nameof(AdjustVariableName), nameof(AdjustVariableAmountText), nameof(LogMessageEdit),
        })
            OnPropertyChanged(name);
    }
}
