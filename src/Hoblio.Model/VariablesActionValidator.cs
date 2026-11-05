namespace Hoblio.Model;

public static class VariablesActionValidator
{
    public static void Validate(VariablesAction action, string nodeId)
    {
        var type = action.Operation;
        switch (type)
        {
            case VariablesActionTypes.CreateDatatable:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                break;
            case VariablesActionTypes.AddRow:
            case VariablesActionTypes.DeleteEmptyRows:
            case VariablesActionTypes.DeleteDuplicateRows:
            case VariablesActionTypes.ClearDatatable:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                break;
            case VariablesActionTypes.DeleteRow:
            case VariablesActionTypes.ModifyCell:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                Require(action.RowIndex, nodeId, "row_index");
                break;
            case VariablesActionTypes.AddColumn:
            case VariablesActionTypes.DeleteColumn:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                if (string.IsNullOrWhiteSpace(action.ColumnName) && string.IsNullOrWhiteSpace(action.ColumnIndex))
                    throw Required(nodeId, "column_name or column_index");
                break;
            case VariablesActionTypes.FindReplaceDatatable:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                Require(action.FindText, nodeId, "find_text");
                break;
            case VariablesActionTypes.SortDatatable:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                if (string.IsNullOrWhiteSpace(action.SortColumn) && string.IsNullOrWhiteSpace(action.ColumnName))
                    throw Required(nodeId, "sort_column or column_name");
                break;
            case VariablesActionTypes.FilterDatatable:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                if (string.IsNullOrWhiteSpace(action.FilterColumn) && string.IsNullOrWhiteSpace(action.FilterConditions))
                    throw Required(nodeId, "filter_column or filter_conditions");
                Require(action.OutputVariable, nodeId, "output_variable");
                break;
            case VariablesActionTypes.MergeDatatables:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                Require(action.SecondDataTableVariable, nodeId, "second_data_table_variable");
                break;
            case VariablesActionTypes.JoinDatatable:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                Require(action.SecondDataTableVariable, nodeId, "second_data_table_variable");
                Require(action.JoinColumn, nodeId, "join_column");
                break;
            case VariablesActionTypes.ReadCsv:
                Require(action.Path, nodeId, "path");
                Require(action.DataTableVariable ?? action.OutputVariable, nodeId, "data_table_variable");
                if (IsFixedWidth(action.Mode))
                    Require(action.ColumnWidths, nodeId, "column_widths");
                break;
            case VariablesActionTypes.WriteCsv:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                Require(action.Path, nodeId, "path");
                break;
            case VariablesActionTypes.ColumnToList:
                Require(action.DataTableVariable, nodeId, "data_table_variable");
                Require(action.ListVariable ?? action.OutputVariable, nodeId, "list_variable");
                if (string.IsNullOrWhiteSpace(action.ColumnName) && string.IsNullOrWhiteSpace(action.ColumnIndex))
                    throw Required(nodeId, "column_name or column_index");
                break;
            case VariablesActionTypes.TruncateNumber:
            case VariablesActionTypes.RandomNumber:
                Require(action.OutputVariable, nodeId, "output_variable");
                break;
            case VariablesActionTypes.CreateList:
                Require(action.ListVariable, nodeId, "list_variable");
                break;
            case VariablesActionTypes.RemoveListItem:
            case VariablesActionTypes.SortList:
            case VariablesActionTypes.ShuffleList:
            case VariablesActionTypes.ReverseList:
            case VariablesActionTypes.RemoveDuplicateListItems:
                Require(action.ListVariable, nodeId, "list_variable");
                break;
            case VariablesActionTypes.MergeLists:
            case VariablesActionTypes.FindCommonListItems:
            case VariablesActionTypes.SubtractLists:
                Require(action.ListVariable, nodeId, "list_variable");
                Require(action.SecondListVariable, nodeId, "second_list_variable");
                break;
            case VariablesActionTypes.FilterList:
                Require(action.ListVariable, nodeId, "list_variable");
                break;
            case VariablesActionTypes.JsonToCustomObject:
                Require(action.OutputVariable, nodeId, "output_variable");
                if (string.IsNullOrWhiteSpace(action.JsonText) && string.IsNullOrWhiteSpace(action.SourceVariable))
                    throw Required(nodeId, "json_text or source_variable");
                break;
            case VariablesActionTypes.CustomObjectToJson:
                Require(action.SourceVariable, nodeId, "source_variable");
                Require(action.OutputVariable, nodeId, "output_variable");
                break;
        }
    }

    static bool IsFixedWidth(string? mode)
    {
        mode = mode?.Trim().ToLowerInvariant();
        return mode is "fixed_width" or "fixedwidth" or "fixed_column_widths" or "fixed";
    }

    static void Require(string? value, string nodeId, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Required(nodeId, field);
    }

    static ValidationException Required(string nodeId, string field) =>
        new(ValidationErrorKind.ActionNodeMissingAction, $"node `{nodeId}` variables action requires `{field}`");
}
