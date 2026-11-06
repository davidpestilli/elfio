using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public bool ShowActionLlmCompletion => GetSelectedNode()?.Action is LlmCompletionAction;

    LlmCompletionAction? LlmA() => A<LlmCompletionAction>();

    void PatchLlm(Func<LlmCompletionAction, LlmCompletionAction> patch)
    {
        if (LlmA() is not { } a)
            return;
        SetAction(patch(a));
    }

    public string LlmCompletionUrl
    {
        get => LlmA()?.Url ?? "";
        set => PatchLlm(a => new LlmCompletionAction
        {
            Url = value,
            ApiStyle = a.ApiStyle,
            Model = a.Model,
            System = a.System,
            User = a.User,
            Document = a.Document,
            JsonSchema = a.JsonSchema,
            Temperature = a.Temperature,
            ResponseBodyVariable = a.ResponseBodyVariable,
        });
    }

    public string LlmCompletionApiStyle
    {
        get => string.IsNullOrWhiteSpace(LlmA()?.ApiStyle) ? "ollama_chat" : LlmA()!.ApiStyle!;
        set => PatchLlm(a => new LlmCompletionAction
        {
            Url = a.Url,
            ApiStyle = string.IsNullOrWhiteSpace(value) ? "ollama_chat" : value.Trim(),
            Model = a.Model,
            System = a.System,
            User = a.User,
            Document = a.Document,
            JsonSchema = a.JsonSchema,
            Temperature = a.Temperature,
            ResponseBodyVariable = a.ResponseBodyVariable,
        });
    }

    public string LlmCompletionModel
    {
        get => LlmA()?.Model ?? "";
        set => PatchLlm(a => new LlmCompletionAction
        {
            Url = a.Url,
            ApiStyle = a.ApiStyle,
            Model = value,
            System = a.System,
            User = a.User,
            Document = a.Document,
            JsonSchema = a.JsonSchema,
            Temperature = a.Temperature,
            ResponseBodyVariable = a.ResponseBodyVariable,
        });
    }

    public string LlmCompletionSystem
    {
        get => LlmA()?.System ?? "";
        set => PatchLlm(a => new LlmCompletionAction
        {
            Url = a.Url,
            ApiStyle = a.ApiStyle,
            Model = a.Model,
            System = string.IsNullOrWhiteSpace(value) ? null : value,
            User = a.User,
            Document = a.Document,
            JsonSchema = a.JsonSchema,
            Temperature = a.Temperature,
            ResponseBodyVariable = a.ResponseBodyVariable,
        });
    }

    public string LlmCompletionUser
    {
        get => LlmA()?.User ?? "";
        set => PatchLlm(a => new LlmCompletionAction
        {
            Url = a.Url,
            ApiStyle = a.ApiStyle,
            Model = a.Model,
            System = a.System,
            User = string.IsNullOrWhiteSpace(value) ? null : value,
            Document = a.Document,
            JsonSchema = a.JsonSchema,
            Temperature = a.Temperature,
            ResponseBodyVariable = a.ResponseBodyVariable,
        });
    }

    public string LlmCompletionDocument
    {
        get => LlmA()?.Document ?? "";
        set => PatchLlm(a => new LlmCompletionAction
        {
            Url = a.Url,
            ApiStyle = a.ApiStyle,
            Model = a.Model,
            System = a.System,
            User = a.User,
            Document = string.IsNullOrWhiteSpace(value) ? null : value,
            JsonSchema = a.JsonSchema,
            Temperature = a.Temperature,
            ResponseBodyVariable = a.ResponseBodyVariable,
        });
    }

    public string LlmCompletionJsonSchema
    {
        get => LlmA()?.JsonSchema ?? "";
        set => PatchLlm(a => new LlmCompletionAction
        {
            Url = a.Url,
            ApiStyle = a.ApiStyle,
            Model = a.Model,
            System = a.System,
            User = a.User,
            Document = a.Document,
            JsonSchema = string.IsNullOrWhiteSpace(value) ? null : value,
            Temperature = a.Temperature,
            ResponseBodyVariable = a.ResponseBodyVariable,
        });
    }

    public string LlmCompletionTemperature
    {
        get => LlmA()?.Temperature ?? "";
        set => PatchLlm(a => new LlmCompletionAction
        {
            Url = a.Url,
            ApiStyle = a.ApiStyle,
            Model = a.Model,
            System = a.System,
            User = a.User,
            Document = a.Document,
            JsonSchema = a.JsonSchema,
            Temperature = string.IsNullOrWhiteSpace(value) ? null : value,
            ResponseBodyVariable = a.ResponseBodyVariable,
        });
    }

    public string LlmCompletionResponseBodyVariable
    {
        get => LlmA()?.ResponseBodyVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || LlmA() is not { } a)
                return;
            ApplyProducerRenameOr(
                node,
                () => node.Action = new LlmCompletionAction
                {
                    Url = a.Url,
                    ApiStyle = a.ApiStyle,
                    Model = a.Model,
                    System = a.System,
                    User = a.User,
                    Document = a.Document,
                    JsonSchema = a.JsonSchema,
                    Temperature = a.Temperature,
                    ResponseBodyVariable = value,
                },
                a.ResponseBodyVariable,
                value);
        }
    }

    void RefreshLlmCompletionInspector()
    {
        OnPropertyChanged(nameof(ShowActionLlmCompletion));
        foreach (var name in new[]
                 {
                     nameof(LlmCompletionUrl), nameof(LlmCompletionApiStyle), nameof(LlmCompletionModel),
                     nameof(LlmCompletionSystem), nameof(LlmCompletionUser), nameof(LlmCompletionDocument),
                     nameof(LlmCompletionJsonSchema), nameof(LlmCompletionTemperature), nameof(LlmCompletionResponseBodyVariable),
                 })
            OnPropertyChanged(name);
    }
}
