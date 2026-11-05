using Hoblio.Model;

namespace Hoblio.Studio;

static class VariablesActionDefaults
{
    public static VariablesAction? TryCreate(string type, IReadOnlySet<string>? takenNames)
    {
        if (!VariablesActionTypes.IsVariablesType(type))
            return null;

        takenNames ??= new HashSet<string>();
        var table = NextName("DataTable", takenNames) ?? "DataTable1";
        var list = NextName("List", takenNames) ?? "List1";
        var output = NextName("Result", takenNames) ?? "Result1";

        return type switch
        {
            VariablesActionTypes.CreateDatatable => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                Values = "Column1,Column2,Column3",
            },
            VariablesActionTypes.AddRow => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                Values = "value1,value2,value3",
                Mode = VariablesEnums.AddRowMode.Append,
            },
            VariablesActionTypes.DeleteRow => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                RowIndex = "0",
            },
            VariablesActionTypes.ModifyCell => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                RowIndex = "0",
                ColumnIndex = "0",
                Value = "new value",
            },
            VariablesActionTypes.FindReplaceDatatable => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                FindText = "find",
                ReplaceText = "replace",
                FindReplaceMode = VariablesEnums.FindReplaceMode.Replace,
            },
            VariablesActionTypes.AddColumn => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                ColumnName = "NewColumn",
                Mode = VariablesEnums.AddColumnMode.Append,
            },
            VariablesActionTypes.DeleteColumn => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                ColumnIndex = "0",
            },
            VariablesActionTypes.DeleteEmptyRows or VariablesActionTypes.DeleteDuplicateRows or VariablesActionTypes.ClearDatatable => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
            },
            VariablesActionTypes.SortDatatable => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                SortColumn = "Column1",
                SortOrder = VariablesEnums.SortDirection.Asc,
            },
            VariablesActionTypes.FilterDatatable => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                FilterColumn = "Column1",
                FilterOperator = VariablesEnums.FilterOperator.Eq,
                FilterValue = "value",
                OutputVariable = output,
            },
            VariablesActionTypes.MergeDatatables => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                SecondDataTableVariable = NextName("DataTable", takenNames) ?? "DataTable2",
                Mode = VariablesEnums.MergeMode.IgnoreExtraColumns,
            },
            VariablesActionTypes.JoinDatatable => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                SecondDataTableVariable = NextName("DataTable", takenNames) ?? "DataTable2",
                JoinColumn = "Column1",
                JoinOperation = VariablesEnums.JoinOperation.Inner,
                OutputVariable = output,
            },
            VariablesActionTypes.ReadCsv => new VariablesAction
            {
                Operation = type,
                Path = "data/input.csv",
                DataTableVariable = table,
                FirstLineIsHeader = true,
                Separator = VariablesEnums.CsvSeparator.Comma,
            },
            VariablesActionTypes.WriteCsv => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                Path = "output/export.csv",
                Separator = VariablesEnums.CsvSeparator.Comma,
            },
            VariablesActionTypes.ColumnToList => new VariablesAction
            {
                Operation = type,
                DataTableVariable = table,
                ColumnIndex = "0",
                ListVariable = list,
            },
            VariablesActionTypes.TruncateNumber => new VariablesAction
            {
                Operation = type,
                SourceVariable = "counter",
                Mode = VariablesEnums.TruncateOperation.Round,
                DecimalPlaces = "2",
                OutputVariable = output,
            },
            VariablesActionTypes.RandomNumber => new VariablesAction
            {
                Operation = type,
                MinValue = "0",
                MaxValue = "100",
                OutputVariable = output,
            },
            VariablesActionTypes.CreateList => new VariablesAction
            {
                Operation = type,
                ListVariable = list,
                Items = "item1\nitem2\nitem3",
            },
            VariablesActionTypes.RemoveListItem => new VariablesAction
            {
                Operation = type,
                ListVariable = list,
                Mode = VariablesEnums.RemoveListItemMode.ByIndex,
                ItemIndex = "0",
            },
            VariablesActionTypes.SortList or VariablesActionTypes.ReverseList or VariablesActionTypes.ShuffleList
                or VariablesActionTypes.RemoveDuplicateListItems => new VariablesAction
            {
                Operation = type,
                ListVariable = list,
            },
            VariablesActionTypes.MergeLists or VariablesActionTypes.FindCommonListItems or VariablesActionTypes.SubtractLists => new VariablesAction
            {
                Operation = type,
                ListVariable = list,
                SecondListVariable = NextName("List", takenNames) ?? "List2",
                OutputVariable = output,
            },
            VariablesActionTypes.FilterList => new VariablesAction
            {
                Operation = type,
                ListVariable = list,
                Value = "filter",
                OutputVariable = output,
            },
            VariablesActionTypes.JsonToCustomObject => new VariablesAction
            {
                Operation = type,
                JsonText = "{\"key\":\"value\"}",
                OutputVariable = output,
            },
            VariablesActionTypes.CustomObjectToJson => new VariablesAction
            {
                Operation = type,
                SourceVariable = output,
                OutputVariable = NextName("Json", takenNames) ?? "Json1",
            },
            _ => new VariablesAction { Operation = type },
        };
    }

    static string? NextName(string prefix, IReadOnlySet<string> taken)
    {
        for (var i = 1; i <= 99; i++)
        {
            var name = prefix + i;
            if (!taken.Contains(name))
                return name;
        }
        return null;
    }
}
