using System.Text.Json;

namespace Hoblio.Model.Json;

static class WebAutomationActionJson
{
    public static WebAutomationAction Read(JsonElement root, string type)
    {
        if (!WebAutomationActionTypes.IsWebAutomationType(type))
            throw new JsonException($"unknown web automation action `{type}`");

        return new WebAutomationAction
        {
            Operation = type,
            Url = GetString(root, "url"),
            WaitMs = GetUInt64(root, "wait_ms"),
            ShowOnScreen = GetBool(root, "show_on_screen"),
            StartMaximized = GetBool(root, "start_maximized"),
            Selector = GetString(root, "selector"),
            TimeoutMs = GetUInt64(root, "timeout_ms"),
            WaitForNavigation = GetBool(root, "wait_for_navigation"),
            WaitSelectorAfter = GetString(root, "wait_selector_after"),
            WaitAfterTimeoutMs = GetUInt64(root, "wait_after_timeout_ms"),
            DialogAction = GetString(root, "dialog_action"),
            Text = GetString(root, "text"),
            PopulateMode = GetString(root, "populate_mode"),
            WaitVisible = GetBool(root, "wait_visible"),
            Variable = GetString(root, "variable"),
            Detail = GetString(root, "detail"),
            Script = GetString(root, "script"),
            Path = GetString(root, "path"),
            Checked = GetBool(root, "checked"),
            DropdownMode = GetString(root, "dropdown_mode"),
            DropdownValue = GetString(root, "dropdown_value"),
            WaitContentMode = GetString(root, "wait_content_mode"),
            BrowserEngine = GetString(root, "browser_engine"),
            AttachMode = GetString(root, "attach_mode"),
            DebuggingPort = GetUInt64(root, "debugging_port"),
        };
    }

    public static void Write(Utf8JsonWriter writer, WebAutomationAction a)
    {
        WriteOptionalString(writer, "url", a.Url);
        WriteOptionalUInt64(writer, "wait_ms", a.WaitMs);
        WriteOptionalBool(writer, "show_on_screen", a.ShowOnScreen);
        WriteOptionalBool(writer, "start_maximized", a.StartMaximized);
        WriteOptionalString(writer, "selector", a.Selector);
        WriteOptionalUInt64(writer, "timeout_ms", a.TimeoutMs);
        WriteOptionalBool(writer, "wait_for_navigation", a.WaitForNavigation);
        WriteOptionalString(writer, "wait_selector_after", a.WaitSelectorAfter);
        WriteOptionalUInt64(writer, "wait_after_timeout_ms", a.WaitAfterTimeoutMs);
        WriteOptionalString(writer, "dialog_action", a.DialogAction);
        WriteOptionalString(writer, "text", a.Text);
        WriteOptionalString(writer, "populate_mode", a.PopulateMode);
        WriteOptionalBool(writer, "wait_visible", a.WaitVisible);
        WriteOptionalString(writer, "variable", a.Variable);
        WriteOptionalString(writer, "detail", a.Detail);
        WriteOptionalString(writer, "script", a.Script);
        WriteOptionalString(writer, "path", a.Path);
        WriteOptionalBool(writer, "checked", a.Checked);
        WriteOptionalString(writer, "dropdown_mode", a.DropdownMode);
        WriteOptionalString(writer, "dropdown_value", a.DropdownValue);
        WriteOptionalString(writer, "wait_content_mode", a.WaitContentMode);
        WriteOptionalString(writer, "browser_engine", a.BrowserEngine);
        WriteOptionalString(writer, "attach_mode", a.AttachMode);
        WriteOptionalUInt64(writer, "debugging_port", a.DebuggingPort);
    }

    static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    static bool? GetBool(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? el.GetBoolean()
            : null;

    static ulong? GetUInt64(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetUInt64() : null;

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
}
