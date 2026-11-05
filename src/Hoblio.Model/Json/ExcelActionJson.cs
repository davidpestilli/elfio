using System.Text.Json;

namespace Hoblio.Model.Json;

static class ExcelActionJson
{
    public static ExcelAction Read(JsonElement root, string type)
    {
        if (!ExcelActionTypes.IsExcelType(type))
            throw new JsonException($"unknown excel action `{type}`");

        return new ExcelAction
        {
            Operation = type,
            Instance = GetString(root, "instance") ?? "ExcelInstance",
            WorksheetName = GetString(root, "worksheet_name"),
            WorksheetIndex = GetString(root, "worksheet_index"),
            Path = GetString(root, "path"),
            Visible = GetBool(root, "visible"),
            ReadOnly = GetBool(root, "read_only"),
            DocumentName = GetString(root, "document_name"),
            StartRow = GetString(root, "start_row"),
            StartColumn = GetString(root, "start_column"),
            EndRow = GetString(root, "end_row"),
            EndColumn = GetString(root, "end_column"),
            CellName = GetString(root, "cell_name"),
            Value = GetString(root, "value"),
            OutputVariable = GetString(root, "output_variable"),
            OutputVariable2 = GetString(root, "output_variable2"),
            Index = GetString(root, "index"),
            Macro = GetString(root, "macro"),
            FirstLineIsHeader = GetBool(root, "first_line_is_header"),
            SaveOnClose = GetBool(root, "save_on_close"),
            NewName = GetString(root, "new_name"),
            FindText = GetString(root, "find_text"),
            ReplaceText = GetString(root, "replace_text"),
            ColorHex = GetString(root, "color_hex"),
            Mode = GetString(root, "mode"),
            TableName = GetString(root, "table_name"),
            TargetInstance = GetString(root, "target_instance"),
            SourceWorksheetName = GetString(root, "source_worksheet_name"),
            TargetWorksheetName = GetString(root, "target_worksheet_name"),
            WorksheetPosition = GetString(root, "worksheet_position"),
            AdvancedFilter = GetBool(root, "advanced_filter"),
            FilterOperator1 = GetString(root, "filter_operator1"),
            FilterValue1 = GetString(root, "filter_value1"),
            FilterOperator2 = GetString(root, "filter_operator2"),
            FilterValue2 = GetString(root, "filter_value2"),
            FilterLogic = GetString(root, "filter_logic"),
            PasteMode = GetString(root, "paste_mode"),
            CopyMode = GetString(root, "copy_mode"),
            ClearFilterScope = GetString(root, "clear_filter_scope"),
            DeleteScope = GetString(root, "delete_scope"),
            SortColumn2 = GetString(root, "sort_column2"),
            SortOrder2 = GetString(root, "sort_order2"),
            MatchCase = GetBool(root, "match_case"),
            MatchEntireCell = GetBool(root, "match_entire_cell"),
            MatchExact = GetBool(root, "match_exact"),
        };
    }

    public static void Write(Utf8JsonWriter writer, ExcelAction a)
    {
        writer.WriteString("instance", a.Instance);
        WriteOptionalString(writer, "worksheet_name", a.WorksheetName);
        WriteOptionalString(writer, "worksheet_index", a.WorksheetIndex);
        WriteOptionalString(writer, "path", a.Path);
        WriteOptionalBool(writer, "visible", a.Visible);
        WriteOptionalBool(writer, "read_only", a.ReadOnly);
        WriteOptionalString(writer, "document_name", a.DocumentName);
        WriteOptionalString(writer, "start_row", a.StartRow);
        WriteOptionalString(writer, "start_column", a.StartColumn);
        WriteOptionalString(writer, "end_row", a.EndRow);
        WriteOptionalString(writer, "end_column", a.EndColumn);
        WriteOptionalString(writer, "cell_name", a.CellName);
        WriteOptionalString(writer, "value", a.Value);
        WriteOptionalString(writer, "output_variable", a.OutputVariable);
        WriteOptionalString(writer, "output_variable2", a.OutputVariable2);
        WriteOptionalString(writer, "index", a.Index);
        WriteOptionalString(writer, "macro", a.Macro);
        WriteOptionalBool(writer, "first_line_is_header", a.FirstLineIsHeader);
        WriteOptionalBool(writer, "save_on_close", a.SaveOnClose);
        WriteOptionalString(writer, "new_name", a.NewName);
        WriteOptionalString(writer, "find_text", a.FindText);
        WriteOptionalString(writer, "replace_text", a.ReplaceText);
        WriteOptionalString(writer, "color_hex", a.ColorHex);
        WriteOptionalString(writer, "mode", a.Mode);
        WriteOptionalString(writer, "table_name", a.TableName);
        WriteOptionalString(writer, "target_instance", a.TargetInstance);
        WriteOptionalString(writer, "source_worksheet_name", a.SourceWorksheetName);
        WriteOptionalString(writer, "target_worksheet_name", a.TargetWorksheetName);
        WriteOptionalString(writer, "worksheet_position", a.WorksheetPosition);
        WriteOptionalBool(writer, "advanced_filter", a.AdvancedFilter);
        WriteOptionalString(writer, "filter_operator1", a.FilterOperator1);
        WriteOptionalString(writer, "filter_value1", a.FilterValue1);
        WriteOptionalString(writer, "filter_operator2", a.FilterOperator2);
        WriteOptionalString(writer, "filter_value2", a.FilterValue2);
        WriteOptionalString(writer, "filter_logic", a.FilterLogic);
        WriteOptionalString(writer, "paste_mode", a.PasteMode);
        WriteOptionalString(writer, "copy_mode", a.CopyMode);
        WriteOptionalString(writer, "clear_filter_scope", a.ClearFilterScope);
        WriteOptionalString(writer, "delete_scope", a.DeleteScope);
        WriteOptionalString(writer, "sort_column2", a.SortColumn2);
        WriteOptionalString(writer, "sort_order2", a.SortOrder2);
        WriteOptionalBool(writer, "match_case", a.MatchCase);
        WriteOptionalBool(writer, "match_entire_cell", a.MatchEntireCell);
        WriteOptionalBool(writer, "match_exact", a.MatchExact);
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
