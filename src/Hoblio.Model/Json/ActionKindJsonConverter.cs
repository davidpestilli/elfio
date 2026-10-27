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
            "browser_navigate" => new BrowserNavigateAction
            {
                Url = root.GetProperty("url").GetString() ?? "",
                WaitMs = GetUInt64(root, "wait_ms"),
                ShowOnScreen = GetBool(root, "show_on_screen"),
                StartMaximized = GetBool(root, "start_maximized"),
            },
            "browser_click" => new BrowserClickAction
            {
                Selector = root.GetProperty("selector").GetString() ?? "",
                TimeoutMs = GetUInt64(root, "timeout_ms"),
                WaitForNavigation = GetBool(root, "wait_for_navigation"),
                WaitSelectorAfter = GetString(root, "wait_selector_after"),
                WaitAfterTimeoutMs = GetUInt64(root, "wait_after_timeout_ms"),
            },
            "browser_wait_navigation" => new BrowserWaitNavigationAction
            {
                TimeoutMs = GetUInt64(root, "timeout_ms"),
            },
            "browser_fill" => new BrowserFillAction
            {
                Selector = root.GetProperty("selector").GetString() ?? "",
                Text = root.GetProperty("text").GetString() ?? "",
                TimeoutMs = GetUInt64(root, "timeout_ms"),
            },
            "browser_wait_for" => new BrowserWaitForAction
            {
                Selector = root.GetProperty("selector").GetString() ?? "",
                TimeoutMs = GetUInt64(root, "timeout_ms"),
            },
            "browser_get_text" => new BrowserGetTextAction
            {
                Selector = root.GetProperty("selector").GetString() ?? "",
                TimeoutMs = GetUInt64(root, "timeout_ms"),
                Variable = GetString(root, "variable"),
            },
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
            case BrowserNavigateAction a:
                writer.WriteString("url", a.Url);
                WriteOptionalUInt64(writer, "wait_ms", a.WaitMs);
                WriteOptionalBool(writer, "show_on_screen", a.ShowOnScreen);
                WriteOptionalBool(writer, "start_maximized", a.StartMaximized);
                break;
            case BrowserClickAction a:
                writer.WriteString("selector", a.Selector);
                WriteOptionalUInt64(writer, "timeout_ms", a.TimeoutMs);
                WriteOptionalBool(writer, "wait_for_navigation", a.WaitForNavigation);
                WriteOptionalString(writer, "wait_selector_after", a.WaitSelectorAfter);
                WriteOptionalUInt64(writer, "wait_after_timeout_ms", a.WaitAfterTimeoutMs);
                break;
            case BrowserWaitNavigationAction a:
                WriteOptionalUInt64(writer, "timeout_ms", a.TimeoutMs);
                break;
            case BrowserFillAction a:
                writer.WriteString("selector", a.Selector);
                writer.WriteString("text", a.Text);
                WriteOptionalUInt64(writer, "timeout_ms", a.TimeoutMs);
                break;
            case BrowserWaitForAction a:
                writer.WriteString("selector", a.Selector);
                WriteOptionalUInt64(writer, "timeout_ms", a.TimeoutMs);
                break;
            case BrowserGetTextAction a:
                writer.WriteString("selector", a.Selector);
                WriteOptionalUInt64(writer, "timeout_ms", a.TimeoutMs);
                WriteOptionalString(writer, "variable", a.Variable);
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
        }
        writer.WriteEndObject();
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
