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
                WebAutomationAction w => $"Web: {WebAutomationActionTypes.ModuleActionId(w.Type)}",
                RunFlowAction a => $"RunFlow: {a.FlowId ?? a.Path ?? "?"}",
                SetVariableAction a => $"Set {a.Variable}",
                ShowGetUserInputAction a => $"Input: {Truncate(a.Title, 30)}",
                ShowSelectFromListAction a => $"Lista: {Truncate(a.Title, 30)}",
                ExcelAction a => $"Excel: {ExcelActionTypes.ModuleActionId(a.Type)}",
                WordAction a => $"Word: {WordActionTypes.ModuleActionId(a.Type)}",
                PdfAction a => $"PDF: {PdfActionTypes.ModuleActionId(a.Type)}",
                VariablesAction a => $"Variáveis: {VariablesActionTypes.ModuleActionId(a.Type)}",
                HttpRequestAction a => $"HTTP {a.Method}: {Truncate(a.Url, 36)}",
                LlmCompletionAction l => $"LLM ({l.ApiStyle}) {Truncate(l.Model, 28)}",
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
    public bool ShowActionHttpRequest => GetSelectedNode()?.Action is HttpRequestAction;
    public bool ShowActionShowMessage => GetSelectedNode()?.Action is ShowMessageAction;
    public bool ShowActionShowGetUserInput => GetSelectedNode()?.Action is ShowGetUserInputAction;
    public bool ShowActionShowSelectFromList => GetSelectedNode()?.Action is ShowSelectFromListAction;
    public bool ShowActionDelay => GetSelectedNode()?.Action is DelayAction;
    public bool ShowActionAdjustVariable => GetSelectedNode()?.Action is IncreaseVariableAction or DecreaseVariableAction;
    public bool ShowActionSetVariable => GetSelectedNode()?.Action is SetVariableAction;

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

    HttpRequestAction PatchHttpRequest(HttpRequestAction a,
        string? url = null,
        string? method = null,
        string? accept = null,
        string? body = null,
        string? contentType = null,
        string? customHeaders = null,
        string? responseBodyVariable = null,
        string? responseHeadersVariable = null,
        string? statusCodeVariable = null) =>
        new()
        {
            Url = url ?? a.Url,
            Method = method ?? a.Method,
            Accept = accept ?? a.Accept,
            Body = body ?? a.Body,
            ContentType = contentType ?? a.ContentType,
            CustomHeaders = customHeaders ?? a.CustomHeaders,
            ResponseBodyVariable = responseBodyVariable ?? a.ResponseBodyVariable,
            ResponseHeadersVariable = responseHeadersVariable ?? a.ResponseHeadersVariable,
            StatusCodeVariable = statusCodeVariable ?? a.StatusCodeVariable,
        };

    public string HttpRequestUrl
    {
        get => A<HttpRequestAction>()?.Url ?? "";
        set
        {
            if (A<HttpRequestAction>() is { } a)
                SetAction(PatchHttpRequest(a, url: value));
            else
                SetAction(new HttpRequestAction { Url = value, Method = "get" });
        }
    }

    public string HttpRequestMethod
    {
        get => A<HttpRequestAction>()?.Method ?? "get";
        set
        {
            if (A<HttpRequestAction>() is not { } a)
                return;
            SetAction(PatchHttpRequest(a, method: string.IsNullOrWhiteSpace(value) ? "get" : value.Trim()));
        }
    }

    public string HttpRequestAccept
    {
        get => A<HttpRequestAction>()?.Accept ?? "";
        set
        {
            if (A<HttpRequestAction>() is not { } a)
                return;
            SetAction(PatchHttpRequest(a, accept: string.IsNullOrWhiteSpace(value) ? null : value));
        }
    }

    public string HttpRequestBody
    {
        get => A<HttpRequestAction>()?.Body ?? "";
        set
        {
            if (A<HttpRequestAction>() is not { } a)
                return;
            SetAction(PatchHttpRequest(a, body: string.IsNullOrWhiteSpace(value) ? null : value));
        }
    }

    public string HttpRequestContentType
    {
        get => A<HttpRequestAction>()?.ContentType ?? "";
        set
        {
            if (A<HttpRequestAction>() is not { } a)
                return;
            SetAction(PatchHttpRequest(a, contentType: string.IsNullOrWhiteSpace(value) ? null : value));
        }
    }

    public string HttpRequestCustomHeaders
    {
        get => A<HttpRequestAction>()?.CustomHeaders ?? "";
        set
        {
            if (A<HttpRequestAction>() is not { } a)
                return;
            SetAction(PatchHttpRequest(a, customHeaders: string.IsNullOrWhiteSpace(value) ? null : value));
        }
    }

    public string HttpRequestResponseBodyVariable
    {
        get => A<HttpRequestAction>()?.ResponseBodyVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<HttpRequestAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node,
                () => node.Action = PatchHttpRequest(a, responseBodyVariable: string.IsNullOrWhiteSpace(value) ? null : value),
                a.ResponseBodyVariable,
                value);
        }
    }

    public string HttpRequestResponseHeadersVariable
    {
        get => A<HttpRequestAction>()?.ResponseHeadersVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<HttpRequestAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node,
                () => node.Action = PatchHttpRequest(a, responseHeadersVariable: string.IsNullOrWhiteSpace(value) ? null : value),
                a.ResponseHeadersVariable,
                value);
        }
    }

    public string HttpRequestStatusCodeVariable
    {
        get => A<HttpRequestAction>()?.StatusCodeVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<HttpRequestAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node,
                () => node.Action = PatchHttpRequest(a, statusCodeVariable: string.IsNullOrWhiteSpace(value) ? null : value),
                a.StatusCodeVariable,
                value);
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

    public string SelectFromListTitle { get => A<ShowSelectFromListAction>()?.Title ?? ""; set => PatchSelectFromList(a => CopySelectFromList(a, title: value)); }
    public string SelectFromListMessage { get => A<ShowSelectFromListAction>()?.Message ?? ""; set => PatchSelectFromList(a => CopySelectFromList(a, message: value)); }
    public string SelectFromListItems { get => A<ShowSelectFromListAction>()?.List ?? ""; set => PatchSelectFromList(a => CopySelectFromList(a, list: value)); }
    public bool SelectFromListTopmost { get => A<ShowSelectFromListAction>()?.Topmost ?? false; set => PatchSelectFromList(a => CopySelectFromList(a, topmost: value)); }
    public bool SelectFromListLimitToList { get => A<ShowSelectFromListAction>()?.LimitToList ?? true; set => PatchSelectFromList(a => CopySelectFromList(a, limitToList: value)); }
    public bool SelectFromListAllowEmpty { get => A<ShowSelectFromListAction>()?.AllowEmptySelection ?? false; set => PatchSelectFromList(a => CopySelectFromList(a, allowEmptySelection: value)); }
    public bool SelectFromListAllowMultiple { get => A<ShowSelectFromListAction>()?.AllowMultipleSelections ?? false; set => PatchSelectFromList(a => CopySelectFromList(a, allowMultipleSelections: value)); }
    public string SelectFromListSelectedItemVariable
    {
        get => A<ShowSelectFromListAction>()?.SelectedItemVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<ShowSelectFromListAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node, () => node.Action = CopySelectFromList(a, selectedItemVariable: value), a.SelectedItemVariable, value);
        }
    }
    public string SelectFromListSelectedIndexVariable
    {
        get => A<ShowSelectFromListAction>()?.SelectedIndexVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<ShowSelectFromListAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node, () => node.Action = CopySelectFromList(a, selectedIndexVariable: value), a.SelectedIndexVariable, value);
        }
    }
    public string SelectFromListButtonPressedVariable
    {
        get => A<ShowSelectFromListAction>()?.ButtonPressedVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || A<ShowSelectFromListAction>() is not { } a)
                return;
            ApplyProducerRenameOr(node, () => node.Action = CopySelectFromList(a, buttonPressedVariable: value), a.ButtonPressedVariable, value);
        }
    }

    public string DelayMillisecondsText
    {
        get => A<DelayAction>()?.Milliseconds.ToString() ?? "0";
        set => SetAction(new DelayAction { Milliseconds = ulong.TryParse(value, out var ms) ? ms : 0 });
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

    void PatchUserInput(Func<ShowGetUserInputAction, ShowGetUserInputAction> map)
    {
        if (A<ShowGetUserInputAction>() is not { } a)
            return;
        SetAction(map(a));
    }

    void PatchSelectFromList(Func<ShowSelectFromListAction, ShowSelectFromListAction> map)
    {
        if (A<ShowSelectFromListAction>() is not { } a)
            return;
        SetAction(map(a));
    }

    static ShowSelectFromListAction CopySelectFromList(
        ShowSelectFromListAction a,
        string? title = null,
        string? message = null,
        string? list = null,
        bool? topmost = null,
        bool? limitToList = null,
        bool? allowEmptySelection = null,
        bool? allowMultipleSelections = null,
        string? selectedItemVariable = null,
        string? selectedIndexVariable = null,
        string? buttonPressedVariable = null) =>
        new()
        {
            Title = title ?? a.Title,
            Message = message ?? a.Message,
            List = list ?? a.List,
            Topmost = topmost ?? a.Topmost,
            LimitToList = limitToList ?? a.LimitToList,
            AllowEmptySelection = allowEmptySelection ?? a.AllowEmptySelection,
            AllowMultipleSelections = allowMultipleSelections ?? a.AllowMultipleSelections,
            SelectedItemVariable = selectedItemVariable ?? a.SelectedItemVariable,
            SelectedIndexVariable = selectedIndexVariable ?? a.SelectedIndexVariable,
            ButtonPressedVariable = buttonPressedVariable ?? a.ButtonPressedVariable,
        };

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
        OnPropertyChanged(nameof(NodeActionSectionTitle));
        OnPropertyChanged(nameof(ActionTemplateHint));
        OnPropertyChanged(nameof(ShowActionTemplateHint));
        OnPropertyChanged(nameof(ShowActionNoOp));
        OnPropertyChanged(nameof(ShowActionFileWrite));
        OnPropertyChanged(nameof(ShowActionFileAppend));
        OnPropertyChanged(nameof(ShowActionFileRead));
        OnPropertyChanged(nameof(ShowActionListClear));
        OnPropertyChanged(nameof(ShowActionListAppend));
        OnPropertyChanged(nameof(ShowActionHttpGet));
        OnPropertyChanged(nameof(ShowActionHttpRequest));
        OnPropertyChanged(nameof(ShowActionLlmCompletion));
        OnPropertyChanged(nameof(ShowActionShowMessage));
        OnPropertyChanged(nameof(ShowActionShowGetUserInput));
        OnPropertyChanged(nameof(ShowActionShowSelectFromList));
        OnPropertyChanged(nameof(ShowActionDelay));
        OnPropertyChanged(nameof(ShowActionAdjustVariable));
        OnPropertyChanged(nameof(ShowActionSetVariable));
        RefreshExcelInspector();
        RefreshWordInspector();
        RefreshPdfInspector();
        RefreshLlmCompletionInspector();
        RefreshVariablesInspector();
        RefreshWebAutomationInspector();
        NotifyAllActionFieldProperties();
    }

    void NotifyAllActionFieldProperties()
    {
        foreach (var name in new[]
        {
            nameof(FileWritePath), nameof(FileWriteContent), nameof(FileAppendPath), nameof(FileAppendContent),
            nameof(FileReadPath), nameof(FileReadVariable), nameof(ListClearVariable), nameof(ListAppendVariable),
            nameof(ListAppendItem), nameof(HttpGetUrl), nameof(HttpGetVariable),
            nameof(HttpRequestUrl), nameof(HttpRequestMethod), nameof(HttpRequestAccept), nameof(HttpRequestBody),
            nameof(HttpRequestContentType), nameof(HttpRequestCustomHeaders),
            nameof(HttpRequestResponseBodyVariable), nameof(HttpRequestResponseHeadersVariable), nameof(HttpRequestStatusCodeVariable),
            nameof(LlmCompletionUrl), nameof(LlmCompletionApiStyle), nameof(LlmCompletionModel), nameof(LlmCompletionSystem),
            nameof(LlmCompletionUser), nameof(LlmCompletionDocument), nameof(LlmCompletionJsonSchema), nameof(LlmCompletionTemperature),
            nameof(LlmCompletionResponseBodyVariable),
            nameof(PdfPath), nameof(PdfMode), nameof(PdfPage), nameof(PdfStartPage), nameof(PdfEndPage), nameof(PdfPassword),
            nameof(PdfOutputPath), nameof(PdfInputPaths), nameof(PdfImageFormat), nameof(PdfRowTolerance), nameof(PdfOutputVariable),
            nameof(ShowMessageTitle), nameof(ShowMessageText),
            nameof(ShowMessageTimeoutText), nameof(UserInputTitle), nameof(UserInputMessage), nameof(UserInputDefault),
            nameof(UserInputKindMode), nameof(UserInputTopmost), nameof(UserInputVariable), nameof(ButtonPressedVariable),
            nameof(SelectFromListTitle), nameof(SelectFromListMessage), nameof(SelectFromListItems),
            nameof(SelectFromListTopmost), nameof(SelectFromListLimitToList), nameof(SelectFromListAllowEmpty),
            nameof(SelectFromListAllowMultiple), nameof(SelectFromListSelectedItemVariable),
            nameof(SelectFromListSelectedIndexVariable), nameof(SelectFromListButtonPressedVariable),
            nameof(DelayMillisecondsText),
            nameof(SetVariableName), nameof(SetVariableValue),
            nameof(AdjustVariableName), nameof(AdjustVariableAmountText), nameof(LogMessageEdit),
        })
            OnPropertyChanged(name);
    }
}
