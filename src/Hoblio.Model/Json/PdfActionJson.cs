using System.Text.Json;

namespace Hoblio.Model.Json;

static class PdfActionJson
{
    public static PdfAction Read(JsonElement root, string type)
    {
        if (!PdfActionTypes.IsPdfType(type))
            throw new JsonException($"unknown pdf action `{type}`");

        return new PdfAction
        {
            Operation = type,
            Path = root.GetProperty("path").GetString() ?? "",
            Mode = GetString(root, "mode"),
            Page = GetString(root, "page"),
            StartPage = GetString(root, "start_page"),
            EndPage = GetString(root, "end_page"),
            Password = GetString(root, "password"),
            OutputPath = GetString(root, "output_path"),
            InputPaths = GetString(root, "input_paths"),
            ImageFormat = GetString(root, "image_format"),
            RowTolerance = GetString(root, "row_tolerance"),
            OutputVariable = GetString(root, "output_variable"),
        };
    }

    public static void Write(Utf8JsonWriter writer, PdfAction a)
    {
        writer.WriteString("path", a.Path);
        WriteOptionalString(writer, "mode", a.Mode);
        WriteOptionalString(writer, "page", a.Page);
        WriteOptionalString(writer, "start_page", a.StartPage);
        WriteOptionalString(writer, "end_page", a.EndPage);
        WriteOptionalString(writer, "password", a.Password);
        WriteOptionalString(writer, "output_path", a.OutputPath);
        WriteOptionalString(writer, "input_paths", a.InputPaths);
        WriteOptionalString(writer, "image_format", a.ImageFormat);
        WriteOptionalString(writer, "row_tolerance", a.RowTolerance);
        WriteOptionalString(writer, "output_variable", a.OutputVariable);
    }

    static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    static void WriteOptionalString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
            writer.WriteString(name, value);
    }
}
