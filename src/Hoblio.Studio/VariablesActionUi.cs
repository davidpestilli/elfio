using Hoblio.Model;

namespace Hoblio.Studio;

/// <summary>Quais grupos de campos do painel aplicam-se a cada <c>variables_*</c>.</summary>
static class VariablesActionUi
{
    static readonly HashSet<string> DatatableTypes =
    [
        VariablesActionTypes.CreateDatatable,
        VariablesActionTypes.AddRow,
        VariablesActionTypes.DeleteRow,
        VariablesActionTypes.ModifyCell,
        VariablesActionTypes.FindReplaceDatatable,
        VariablesActionTypes.AddColumn,
        VariablesActionTypes.DeleteColumn,
        VariablesActionTypes.DeleteEmptyRows,
        VariablesActionTypes.DeleteDuplicateRows,
        VariablesActionTypes.ClearDatatable,
        VariablesActionTypes.SortDatatable,
        VariablesActionTypes.FilterDatatable,
        VariablesActionTypes.MergeDatatables,
        VariablesActionTypes.JoinDatatable,
        VariablesActionTypes.ReadCsv,
        VariablesActionTypes.WriteCsv,
        VariablesActionTypes.ColumnToList,
    ];

    static readonly HashSet<string> SecondTableTypes =
    [
        VariablesActionTypes.MergeDatatables,
        VariablesActionTypes.JoinDatatable,
    ];

    static readonly HashSet<string> ListTypes =
    [
        VariablesActionTypes.CreateList,
        VariablesActionTypes.RemoveListItem,
        VariablesActionTypes.SortList,
        VariablesActionTypes.ShuffleList,
        VariablesActionTypes.MergeLists,
        VariablesActionTypes.ReverseList,
        VariablesActionTypes.RemoveDuplicateListItems,
        VariablesActionTypes.FindCommonListItems,
        VariablesActionTypes.SubtractLists,
        VariablesActionTypes.FilterList,
        VariablesActionTypes.ColumnToList,
    ];

    static readonly HashSet<string> SecondListTypes =
    [
        VariablesActionTypes.MergeLists,
        VariablesActionTypes.FindCommonListItems,
        VariablesActionTypes.SubtractLists,
    ];

    public static bool UsesDatatable(string? type) => type is not null && DatatableTypes.Contains(type);
    public static bool UsesSecondDatatable(string? type) => type is not null && SecondTableTypes.Contains(type);
    public static bool UsesList(string? type) => type is not null && ListTypes.Contains(type);
    public static bool UsesSecondList(string? type) => type is not null && SecondListTypes.Contains(type);

    public static bool UsesRowValues(string? type) =>
        type is VariablesActionTypes.CreateDatatable or VariablesActionTypes.AddRow;

    public static bool UsesCellCoords(string? type) =>
        type is VariablesActionTypes.ModifyCell
            or VariablesActionTypes.DeleteRow
            or VariablesActionTypes.AddColumn
            or VariablesActionTypes.DeleteColumn
            or VariablesActionTypes.FindReplaceDatatable
            or VariablesActionTypes.FilterDatatable
            or VariablesActionTypes.SortDatatable
            or VariablesActionTypes.ColumnToList;

    public static bool UsesFindReplace(string? type) => type == VariablesActionTypes.FindReplaceDatatable;

    public static bool UsesFilterTable(string? type) => type == VariablesActionTypes.FilterDatatable;

    public static bool UsesFilterList(string? type) => type == VariablesActionTypes.FilterList;

    public static bool UsesSortTable(string? type) => type == VariablesActionTypes.SortDatatable;

    public static bool UsesJoinMerge(string? type) =>
        type is VariablesActionTypes.MergeDatatables or VariablesActionTypes.JoinDatatable;

    public static bool UsesCsv(string? type) =>
        type is VariablesActionTypes.ReadCsv or VariablesActionTypes.WriteCsv;

    public static bool UsesListContent(string? type) => type == VariablesActionTypes.CreateList;

    public static bool UsesListItemOps(string? type) =>
        type is VariablesActionTypes.RemoveListItem or VariablesActionTypes.SortList;

    public static bool UsesNumbers(string? type) =>
        type is VariablesActionTypes.TruncateNumber or VariablesActionTypes.RandomNumber;

    public static bool UsesJson(string? type) =>
        type is VariablesActionTypes.JsonToCustomObject or VariablesActionTypes.CustomObjectToJson;

    public static bool UsesOutputVariable(string? type) =>
        type is VariablesActionTypes.FilterDatatable
            or VariablesActionTypes.JoinDatatable
            or VariablesActionTypes.TruncateNumber
            or VariablesActionTypes.RandomNumber
            or VariablesActionTypes.JsonToCustomObject
            or VariablesActionTypes.CustomObjectToJson
            or VariablesActionTypes.FilterList
            or VariablesActionTypes.MergeLists
            or VariablesActionTypes.FindCommonListItems
            or VariablesActionTypes.SubtractLists
            or VariablesActionTypes.ColumnToList;
}
