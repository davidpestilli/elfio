using System.Text.Json;

namespace Hoblio.Model.Json;

static class VariablesActionJson
{
    public static VariablesAction Read(JsonElement root, string type)
    {
        if (!VariablesActionTypes.IsVariablesType(type))
            throw new JsonException($"unknown variables action `{type}`");

        return new VariablesAction
        {
            Operation = type,
            DataTableVariable = GetString(root, "data_table_variable"),
            SecondDataTableVariable = GetString(root, "second_data_table_variable"),
            ListVariable = GetString(root, "list_variable"),
            SecondListVariable = GetString(root, "second_list_variable"),
            OutputVariable = GetString(root, "output_variable"),
            OutputVariable2 = GetString(root, "output_variable2"),
            Value = GetString(root, "value"),
            Values = GetString(root, "values"),
            Items = GetString(root, "items"),
            ColumnName = GetString(root, "column_name"),
            ColumnIndex = GetString(root, "column_index"),
            RowIndex = GetString(root, "row_index"),
            ColumnSpecifier = GetString(root, "column_specifier"),
            FindText = GetString(root, "find_text"),
            ReplaceText = GetString(root, "replace_text"),
            FindReplaceMode = GetString(root, "find_replace_mode"),
            SearchArea = GetString(root, "search_area"),
            UseRegex = GetBool(root, "use_regex"),
            Mode = GetString(root, "mode"),
            SortColumn = GetString(root, "sort_column"),
            SortOrder = GetString(root, "sort_order"),
            SortColumn2 = GetString(root, "sort_column2"),
            SortOrder2 = GetString(root, "sort_order2"),
            ColumnWidths = GetString(root, "column_widths"),
            FilterColumn = GetString(root, "filter_column"),
            FilterOperator = GetString(root, "filter_operator"),
            FilterValue = GetString(root, "filter_value"),
            FilterColumn2 = GetString(root, "filter_column2"),
            FilterOperator2 = GetString(root, "filter_operator2"),
            FilterValue2 = GetString(root, "filter_value2"),
            FilterLogic = GetString(root, "filter_logic"),
            FilterConditions = GetFilterConditions(root),
            JoinColumn = GetString(root, "join_column"),
            JoinColumn2 = GetString(root, "join_column2"),
            JoinOperation = GetString(root, "join_operation"),
            JoinComparisonOperator = GetString(root, "join_comparison_operator"),
            Path = GetString(root, "path"),
            Separator = GetString(root, "separator"),
            CustomSeparator = GetString(root, "custom_separator"),
            FirstLineIsHeader = GetBool(root, "first_line_is_header"),
            MinValue = GetString(root, "min_value"),
            MaxValue = GetString(root, "max_value"),
            DecimalPlaces = GetString(root, "decimal_places"),
            Count = GetString(root, "count"),
            ItemIndex = GetString(root, "item_index"),
            PropertyName = GetString(root, "property_name"),
            IgnoreCase = GetBool(root, "ignore_case"),
            MatchCase = GetBool(root, "match_case"),
            MatchEntireCell = GetBool(root, "match_entire_cell"),
            RemoveAllMatches = GetBool(root, "remove_all_matches"),
            JsonText = GetString(root, "json_text"),
            SourceVariable = GetString(root, "source_variable"),
        };
    }

    public static void Write(Utf8JsonWriter writer, VariablesAction a)
    {
        WriteOptionalString(writer, "data_table_variable", a.DataTableVariable);
        WriteOptionalString(writer, "second_data_table_variable", a.SecondDataTableVariable);
        WriteOptionalString(writer, "list_variable", a.ListVariable);
        WriteOptionalString(writer, "second_list_variable", a.SecondListVariable);
        WriteOptionalString(writer, "output_variable", a.OutputVariable);
        WriteOptionalString(writer, "output_variable2", a.OutputVariable2);
        WriteOptionalString(writer, "value", a.Value);
        WriteOptionalString(writer, "values", a.Values);
        WriteOptionalString(writer, "items", a.Items);
        WriteOptionalString(writer, "column_name", a.ColumnName);
        WriteOptionalString(writer, "column_index", a.ColumnIndex);
        WriteOptionalString(writer, "row_index", a.RowIndex);
        WriteOptionalString(writer, "column_specifier", a.ColumnSpecifier);
        WriteOptionalString(writer, "find_text", a.FindText);
        WriteOptionalString(writer, "replace_text", a.ReplaceText);
        WriteOptionalString(writer, "find_replace_mode", a.FindReplaceMode);
        WriteOptionalString(writer, "search_area", a.SearchArea);
        WriteOptionalBool(writer, "use_regex", a.UseRegex);
        WriteOptionalString(writer, "mode", a.Mode);
        WriteOptionalString(writer, "sort_column", a.SortColumn);
        WriteOptionalString(writer, "sort_order", a.SortOrder);
        WriteOptionalString(writer, "sort_column2", a.SortColumn2);
        WriteOptionalString(writer, "sort_order2", a.SortOrder2);
        WriteOptionalString(writer, "column_widths", a.ColumnWidths);
        WriteOptionalString(writer, "filter_column", a.FilterColumn);
        WriteOptionalString(writer, "filter_operator", a.FilterOperator);
        WriteOptionalString(writer, "filter_value", a.FilterValue);
        WriteOptionalString(writer, "filter_column2", a.FilterColumn2);
        WriteOptionalString(writer, "filter_operator2", a.FilterOperator2);
        WriteOptionalString(writer, "filter_value2", a.FilterValue2);
        WriteOptionalString(writer, "filter_logic", a.FilterLogic);
        WriteFilterConditions(writer, a.FilterConditions);
        WriteOptionalString(writer, "join_column", a.JoinColumn);
        WriteOptionalString(writer, "join_column2", a.JoinColumn2);
        WriteOptionalString(writer, "join_operation", a.JoinOperation);
        WriteOptionalString(writer, "join_comparison_operator", a.JoinComparisonOperator);
        WriteOptionalString(writer, "path", a.Path);
        WriteOptionalString(writer, "separator", a.Separator);
        WriteOptionalString(writer, "custom_separator", a.CustomSeparator);
        WriteOptionalBool(writer, "first_line_is_header", a.FirstLineIsHeader);
        WriteOptionalString(writer, "min_value", a.MinValue);
        WriteOptionalString(writer, "max_value", a.MaxValue);
        WriteOptionalString(writer, "decimal_places", a.DecimalPlaces);
        WriteOptionalString(writer, "count", a.Count);
        WriteOptionalString(writer, "item_index", a.ItemIndex);
        WriteOptionalString(writer, "property_name", a.PropertyName);
        WriteOptionalBool(writer, "ignore_case", a.IgnoreCase);
        WriteOptionalBool(writer, "match_case", a.MatchCase);
        WriteOptionalBool(writer, "match_entire_cell", a.MatchEntireCell);
        WriteOptionalBool(writer, "remove_all_matches", a.RemoveAllMatches);
        WriteOptionalString(writer, "json_text", a.JsonText);
        WriteOptionalString(writer, "source_variable", a.SourceVariable);
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

    static string? GetFilterConditions(JsonElement root)
    {
        if (!root.TryGetProperty("filter_conditions", out var el))
            return null;
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Array => el.GetRawText(),
            _ => null,
        };
    }

    static void WriteFilterConditions(Utf8JsonWriter writer, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;
        raw = raw.Trim();
        if (raw.StartsWith('['))
        {
            using var doc = JsonDocument.Parse(raw);
            writer.WritePropertyName("filter_conditions");
            doc.RootElement.WriteTo(writer);
            return;
        }
        writer.WriteString("filter_conditions", raw);
    }
}
