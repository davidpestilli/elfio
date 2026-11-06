using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hoblio.Model.Json;

public sealed class ActionKindJsonConverter : JsonConverter<ActionKind>
{
    public override ActionKind? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var typeEl))
            throw new JsonException("action missing `type`");
        var type = typeEl.GetString() ?? throw new JsonException("action `type` is null");

        return type switch
        {
            "log" => new LogAction { Message = root.GetProperty("message").GetString() ?? "" },
            "no_op" => new NoOpAction(),
            _ when WebAutomationActionTypes.IsWebAutomationType(type) => WebAutomationActionJson.Read(root, type),
            "file_write" => new FileWriteAction
            {
                Path = root.GetProperty("path").GetString() ?? "",
                Content = root.GetProperty("content").GetString() ?? "",
            },
            "file_append" => new FileAppendAction
            {
                Path = root.GetProperty("path").GetString() ?? "",
                Content = root.GetProperty("content").GetString() ?? "",
            },
            "file_read" => new FileReadAction
            {
                Path = root.GetProperty("path").GetString() ?? "",
                Variable = root.GetProperty("variable").GetString() ?? "",
            },
            "list_append" => new ListAppendAction
            {
                ListVariable = root.GetProperty("list_variable").GetString() ?? "",
                Item = root.GetProperty("item").GetString() ?? "",
            },
            "list_clear" => new ListClearAction
            {
                ListVariable = root.GetProperty("list_variable").GetString() ?? "",
            },
            "delay" => new DelayAction
            {
                Milliseconds = root.GetProperty("milliseconds").GetUInt64(),
            },
            "http_get" => new HttpGetAction
            {
                Url = root.GetProperty("url").GetString() ?? "",
                Variable = GetString(root, "variable"),
            },
            "http_request" => new HttpRequestAction
            {
                Url = root.GetProperty("url").GetString() ?? "",
                Method = GetString(root, "method") ?? "get",
                Accept = GetString(root, "accept"),
                Body = GetString(root, "body"),
                ContentType = GetString(root, "content_type"),
                CustomHeaders = GetString(root, "custom_headers"),
                ResponseBodyVariable = GetString(root, "response_body_variable"),
                ResponseHeadersVariable = GetString(root, "response_headers_variable"),
                StatusCodeVariable = GetString(root, "status_code_variable"),
            },
            "llm_completion" => new LlmCompletionAction
            {
                Url = root.GetProperty("url").GetString() ?? "",
                ApiStyle = GetString(root, "api_style") ?? "ollama_chat",
                Model = root.GetProperty("model").GetString() ?? "",
                System = GetString(root, "system"),
                User = GetString(root, "user"),
                Document = GetString(root, "document"),
                JsonSchema = GetString(root, "json_schema"),
                Temperature = GetString(root, "temperature"),
                ResponseBodyVariable = root.GetProperty("response_body_variable").GetString() ?? "",
            },
            "show_message" => new ShowMessageAction
            {
                Title = root.GetProperty("title").GetString() ?? "",
                Text = root.GetProperty("text").GetString() ?? "",
                TimeoutSeconds = GetDouble(root, "timeout_seconds"),
            },
            "show_get_user_input" => new ShowGetUserInputAction
            {
                Title = root.GetProperty("title").GetString() ?? "",
                Message = root.GetProperty("message").GetString() ?? "",
                DefaultValue = GetString(root, "default_value") ?? "",
                InputKind = root.TryGetProperty("input_kind", out var ik)
                    ? JsonSerializer.Deserialize<UserInputKind>(ik.GetRawText(), options)
                    : UserInputKind.SingleLine,
                Topmost = GetBool(root, "topmost"),
                UserInputVariable = GetString(root, "user_input_variable") ?? "UserInput",
                ButtonPressedVariable = GetString(root, "button_pressed_variable") ?? "ButtonPressed",
            },
            "show_select_from_list" => new ShowSelectFromListAction
            {
                Title = root.GetProperty("title").GetString() ?? "",
                Message = root.GetProperty("message").GetString() ?? "",
                List = root.GetProperty("list").GetString() ?? "",
                Topmost = GetBool(root, "topmost"),
                LimitToList = GetBool(root, "limit_to_list") ?? true,
                AllowEmptySelection = GetBool(root, "allow_empty_selection"),
                AllowMultipleSelections = GetBool(root, "allow_multiple_selections"),
                SelectedItemVariable = GetString(root, "selected_item_variable") ?? "SelectedItem",
                SelectedIndexVariable = GetString(root, "selected_index_variable") ?? "SelectedIndex",
                ButtonPressedVariable = GetString(root, "button_pressed_variable") ?? "ButtonPressed",
            },
            "set_variable" => new SetVariableAction
            {
                Variable = root.GetProperty("variable").GetString() ?? "",
                Value = root.GetProperty("value").GetString() ?? "",
            },
            "increase_variable" => new IncreaseVariableAction
            {
                Variable = root.GetProperty("variable").GetString() ?? "",
                Amount = GetString(root, "amount"),
            },
            "decrease_variable" => new DecreaseVariableAction
            {
                Variable = root.GetProperty("variable").GetString() ?? "",
                Amount = GetString(root, "amount"),
            },
            "run_flow" => ReadRunFlow(root),
            _ when ExcelActionTypes.IsExcelType(type) => ExcelActionJson.Read(root, type),
            _ when WordActionTypes.IsWordType(type) => WordActionJson.Read(root, type),
            _ when PdfActionTypes.IsPdfType(type) => PdfActionJson.Read(root, type),
            _ when VariablesActionTypes.IsVariablesType(type) => VariablesActionJson.Read(root, type),
            _ => throw new JsonException($"unknown action type `{type}`"),
        };
    }

    public override void Write(Utf8JsonWriter writer, ActionKind value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        switch (value)
        {
            case LogAction a:
                writer.WriteString("message", a.Message);
                break;
            case NoOpAction:
                break;
            case WebAutomationAction a:
                WebAutomationActionJson.Write(writer, a);
                break;
            case FileWriteAction a:
                writer.WriteString("path", a.Path);
                writer.WriteString("content", a.Content);
                break;
            case FileAppendAction a:
                writer.WriteString("path", a.Path);
                writer.WriteString("content", a.Content);
                break;
            case FileReadAction a:
                writer.WriteString("path", a.Path);
                writer.WriteString("variable", a.Variable);
                break;
            case ListAppendAction a:
                writer.WriteString("list_variable", a.ListVariable);
                writer.WriteString("item", a.Item);
                break;
            case ListClearAction a:
                writer.WriteString("list_variable", a.ListVariable);
                break;
            case DelayAction a:
                writer.WriteNumber("milliseconds", a.Milliseconds);
                break;
            case HttpGetAction a:
                writer.WriteString("url", a.Url);
                WriteOptionalString(writer, "variable", a.Variable);
                break;
            case HttpRequestAction a:
                writer.WriteString("url", a.Url);
                writer.WriteString("method", string.IsNullOrWhiteSpace(a.Method) ? "get" : a.Method);
                WriteOptionalString(writer, "accept", a.Accept);
                WriteOptionalString(writer, "body", a.Body);
                WriteOptionalString(writer, "content_type", a.ContentType);
                WriteOptionalString(writer, "custom_headers", a.CustomHeaders);
                WriteOptionalString(writer, "response_body_variable", a.ResponseBodyVariable);
                WriteOptionalString(writer, "response_headers_variable", a.ResponseHeadersVariable);
                WriteOptionalString(writer, "status_code_variable", a.StatusCodeVariable);
                break;
            case LlmCompletionAction a:
                writer.WriteString("url", a.Url);
                writer.WriteString("api_style", string.IsNullOrWhiteSpace(a.ApiStyle) ? "ollama_chat" : a.ApiStyle);
                writer.WriteString("model", a.Model);
                WriteOptionalString(writer, "system", a.System);
                WriteOptionalString(writer, "user", a.User);
                WriteOptionalString(writer, "document", a.Document);
                WriteOptionalString(writer, "json_schema", a.JsonSchema);
                WriteOptionalString(writer, "temperature", a.Temperature);
                writer.WriteString("response_body_variable", a.ResponseBodyVariable);
                break;
            case ShowMessageAction a:
                writer.WriteString("title", a.Title);
                writer.WriteString("text", a.Text);
                WriteOptionalDouble(writer, "timeout_seconds", a.TimeoutSeconds);
                break;
            case ShowGetUserInputAction a:
                writer.WriteString("title", a.Title);
                writer.WriteString("message", a.Message);
                if (!string.IsNullOrEmpty(a.DefaultValue))
                    writer.WriteString("default_value", a.DefaultValue);
                writer.WriteString("input_kind", a.InputKind switch
                {
                    UserInputKind.MultiLine => "multi_line",
                    _ => "single_line",
                });
                WriteOptionalBool(writer, "topmost", a.Topmost);
                writer.WriteString("user_input_variable", a.UserInputVariable);
                writer.WriteString("button_pressed_variable", a.ButtonPressedVariable);
                break;
            case ShowSelectFromListAction a:
                writer.WriteString("title", a.Title);
                writer.WriteString("message", a.Message);
                writer.WriteString("list", a.List);
                WriteOptionalBool(writer, "topmost", a.Topmost);
                WriteOptionalBool(writer, "limit_to_list", a.LimitToList);
                WriteOptionalBool(writer, "allow_empty_selection", a.AllowEmptySelection);
                WriteOptionalBool(writer, "allow_multiple_selections", a.AllowMultipleSelections);
                writer.WriteString("selected_item_variable", a.SelectedItemVariable);
                writer.WriteString("selected_index_variable", a.SelectedIndexVariable);
                writer.WriteString("button_pressed_variable", a.ButtonPressedVariable);
                break;
            case SetVariableAction a:
                writer.WriteString("variable", a.Variable);
                writer.WriteString("value", a.Value);
                break;
            case IncreaseVariableAction a:
                writer.WriteString("variable", a.Variable);
                WriteOptionalString(writer, "amount", a.Amount);
                break;
            case DecreaseVariableAction a:
                writer.WriteString("variable", a.Variable);
                WriteOptionalString(writer, "amount", a.Amount);
                break;
            case ExcelAction a:
                ExcelActionJson.Write(writer, a);
                break;
            case WordAction a:
                WordActionJson.Write(writer, a);
                break;
            case PdfAction a:
                PdfActionJson.Write(writer, a);
                break;
            case VariablesAction a:
                VariablesActionJson.Write(writer, a);
                break;
            case RunFlowAction a:
                WriteOptionalString(writer, "project", a.Project);
                WriteOptionalString(writer, "flow_id", a.FlowId);
                WriteOptionalString(writer, "path", a.Path);
                if (a.Inputs is { Count: > 0 })
                {
                    writer.WritePropertyName("inputs");
                    writer.WriteStartObject();
                    foreach (var (k, v) in a.Inputs)
                        writer.WriteString(k, v);
                    writer.WriteEndObject();
                }
                if (a.Outputs is { Count: > 0 })
                {
                    writer.WritePropertyName("outputs");
                    writer.WriteStartArray();
                    foreach (var o in a.Outputs)
                        writer.WriteStringValue(o);
                    writer.WriteEndArray();
                }
                WriteOptionalBool(writer, "share_browser", a.ShareBrowser);
                if (a.VariableScope is { } scope)
                    writer.WriteString("variable_scope", scope == RunFlowVariableScope.Isolated ? "isolated" : "inherit");
                break;
        }
        writer.WriteEndObject();
    }

    static RunFlowAction ReadRunFlow(JsonElement root)
    {
        Dictionary<string, string>? inputs = null;
        if (root.TryGetProperty("inputs", out var inputsEl) && inputsEl.ValueKind == JsonValueKind.Object)
        {
            inputs = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var prop in inputsEl.EnumerateObject())
                inputs[prop.Name] = prop.Value.GetString() ?? "";
        }
        List<string>? outputs = null;
        if (root.TryGetProperty("outputs", out var outputsEl) && outputsEl.ValueKind == JsonValueKind.Array)
        {
            outputs = [];
            foreach (var item in outputsEl.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    outputs.Add(item.GetString() ?? "");
            }
        }
        RunFlowVariableScope? scope = null;
        if (root.TryGetProperty("variable_scope", out var scopeEl) && scopeEl.ValueKind == JsonValueKind.String)
        {
            scope = scopeEl.GetString() switch
            {
                "isolated" => RunFlowVariableScope.Isolated,
                _ => RunFlowVariableScope.Inherit,
            };
        }
        return new RunFlowAction
        {
            Project = GetString(root, "project"),
            FlowId = GetString(root, "flow_id"),
            Path = GetString(root, "path"),
            Inputs = inputs,
            Outputs = outputs,
            ShareBrowser = GetBool(root, "share_browser"),
            VariableScope = scope,
        };
    }

    static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    static bool? GetBool(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? el.GetBoolean()
            : null;

    static ulong? GetUInt64(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetUInt64() : null;

    static double? GetDouble(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetDouble() : null;

    static void WriteOptionalString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
            writer.WriteString(name, value);
    }

    static void WriteOptionalBool(Utf8JsonWriter writer, string name, bool? value)
    {
        if (value is not null)
            writer.WriteBoolean(name, value.Value);
    }

    static void WriteOptionalUInt64(Utf8JsonWriter writer, string name, ulong? value)
    {
        if (value is not null)
            writer.WriteNumber(name, value.Value);
    }

    static void WriteOptionalDouble(Utf8JsonWriter writer, string name, double? value)
    {
        if (value is not null)
            writer.WriteNumber(name, value.Value);
    }
}
