using System.Text.Json;

namespace Hoblio.Model.Json;

static class WordActionJson
{
    public static WordAction Read(JsonElement root, string type)
    {
        if (!WordActionTypes.IsWordType(type))
            throw new JsonException($"unknown word action `{type}`");

        return new WordAction
        {
            Operation = type,
            Instance = GetString(root, "instance") ?? "WordInstance",
            Path = GetString(root, "path"),
            Visible = GetBool(root, "visible"),
            ReadOnly = GetBool(root, "read_only"),
            DocumentName = GetString(root, "document_name"),
            Mode = GetString(root, "mode"),
            Pages = GetString(root, "pages"),
            Bookmark = GetString(root, "bookmark"),
            Value = GetString(root, "value"),
            AppendNewLine = GetBool(root, "append_new_line"),
            OutputVariable = GetString(root, "output_variable"),
            FindText = GetString(root, "find_text"),
            ReplaceText = GetString(root, "replace_text"),
            AllMatches = GetBool(root, "all_matches"),
            MatchWildcards = GetBool(root, "match_wildcards"),
            MatchCase = GetBool(root, "match_case"),
            MatchEntireWord = GetBool(root, "match_entire_word"),
            ImageSource = GetString(root, "image_source"),
            TextToFind = GetString(root, "text_to_find"),
        };
    }

    public static void Write(Utf8JsonWriter writer, WordAction a)
    {
        writer.WriteString("instance", a.Instance);
        WriteOptionalString(writer, "path", a.Path);
        WriteOptionalBool(writer, "visible", a.Visible);
        WriteOptionalBool(writer, "read_only", a.ReadOnly);
        WriteOptionalString(writer, "document_name", a.DocumentName);
        WriteOptionalString(writer, "mode", a.Mode);
        WriteOptionalString(writer, "pages", a.Pages);
        WriteOptionalString(writer, "bookmark", a.Bookmark);
        WriteOptionalString(writer, "value", a.Value);
        WriteOptionalBool(writer, "append_new_line", a.AppendNewLine);
        WriteOptionalString(writer, "output_variable", a.OutputVariable);
        WriteOptionalString(writer, "find_text", a.FindText);
        WriteOptionalString(writer, "replace_text", a.ReplaceText);
        WriteOptionalBool(writer, "all_matches", a.AllMatches);
        WriteOptionalBool(writer, "match_wildcards", a.MatchWildcards);
        WriteOptionalBool(writer, "match_case", a.MatchCase);
        WriteOptionalBool(writer, "match_entire_word", a.MatchEntireWord);
        WriteOptionalString(writer, "image_source", a.ImageSource);
        WriteOptionalString(writer, "text_to_find", a.TextToFind);
    }

    static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    static bool? GetBool(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? el.GetBoolean()
            : null;

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
}
