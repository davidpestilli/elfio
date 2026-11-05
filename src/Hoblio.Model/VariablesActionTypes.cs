namespace Hoblio.Model;

/// <summary>Identificadores JSON (<c>type</c>) e telemetria (<c>action_id</c>) das ações Variáveis (exceto Assign / Increase / Decrease / list_append / list_clear legados).</summary>
public static class VariablesActionTypes
{
    public const string CreateDatatable = "variables_create_datatable";
    public const string AddRow = "variables_add_row";
    public const string DeleteRow = "variables_delete_row";
    public const string ModifyCell = "variables_modify_cell";
    public const string FindReplaceDatatable = "variables_find_replace_datatable";
    public const string AddColumn = "variables_add_column";
    public const string DeleteColumn = "variables_delete_column";
    public const string DeleteEmptyRows = "variables_delete_empty_rows";
    public const string DeleteDuplicateRows = "variables_delete_duplicate_rows";
    public const string ClearDatatable = "variables_clear_datatable";
    public const string SortDatatable = "variables_sort_datatable";
    public const string FilterDatatable = "variables_filter_datatable";
    public const string MergeDatatables = "variables_merge_datatables";
    public const string JoinDatatable = "variables_join_datatable";
    public const string ReadCsv = "variables_read_csv";
    public const string WriteCsv = "variables_write_csv";
    public const string ColumnToList = "variables_column_to_list";

    public const string TruncateNumber = "variables_truncate_number";
    public const string RandomNumber = "variables_random_number";

    public const string CreateList = "variables_create_list";
    public const string RemoveListItem = "variables_remove_list_item";
    public const string SortList = "variables_sort_list";
    public const string ShuffleList = "variables_shuffle_list";
    public const string MergeLists = "variables_merge_lists";
    public const string ReverseList = "variables_reverse_list";
    public const string RemoveDuplicateListItems = "variables_remove_duplicate_list_items";
    public const string FindCommonListItems = "variables_find_common_list_items";
    public const string SubtractLists = "variables_subtract_lists";
    public const string FilterList = "variables_filter_list";

    public const string JsonToCustomObject = "variables_json_to_custom_object";
    public const string CustomObjectToJson = "variables_custom_object_to_json";

    public static readonly IReadOnlyList<string> All =
    [
        CreateDatatable, AddRow, DeleteRow, ModifyCell, FindReplaceDatatable, AddColumn, DeleteColumn,
        DeleteEmptyRows, DeleteDuplicateRows, ClearDatatable, SortDatatable, FilterDatatable, MergeDatatables,
        JoinDatatable, ReadCsv, WriteCsv, ColumnToList,
        TruncateNumber, RandomNumber,
        CreateList, RemoveListItem, SortList, ShuffleList, MergeLists, ReverseList, RemoveDuplicateListItems,
        FindCommonListItems, SubtractLists, FilterList,
        JsonToCustomObject, CustomObjectToJson,
    ];

    public static readonly IReadOnlyDictionary<string, string> ModuleActionIds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [CreateDatatable] = "CreateNewDatatable",
            [AddRow] = "AddRowToDataTable",
            [DeleteRow] = "DeleteRowFromDataTable",
            [ModifyCell] = "ModifyDataTableItem",
            [FindReplaceDatatable] = "FindOrReplaceInDataTable",
            [AddColumn] = "AddColumnToDataTable",
            [DeleteColumn] = "DeleteColumnFromDataTable",
            [DeleteEmptyRows] = "DeleteEmptyRowsFromDataTable",
            [DeleteDuplicateRows] = "DeleteDuplicateRowsFromDataTable",
            [ClearDatatable] = "ClearDataTable",
            [SortDatatable] = "SortDataTable",
            [FilterDatatable] = "FilterDataTable",
            [MergeDatatables] = "MergeDataTables",
            [JoinDatatable] = "JoinDataTable",
            [ReadCsv] = "GenerateDataTableFromCSV",
            [WriteCsv] = "ConvertDataTableToCSV",
            [ColumnToList] = "RetrieveDataTableColumnIntoList",
            [TruncateNumber] = "TruncateNumber",
            [RandomNumber] = "GenerateRandomNumber",
            [CreateList] = "CreateNewList",
            [RemoveListItem] = "RemoveItemFromList",
            [SortList] = "SortList",
            [ShuffleList] = "ShuffleList",
            [MergeLists] = "MergeLists",
            [ReverseList] = "ReverseList",
            [RemoveDuplicateListItems] = "RemoveDuplicateItemsFromList",
            [FindCommonListItems] = "FindCommonListItems",
            [SubtractLists] = "SubtractLists",
            [FilterList] = "FilterList",
            [JsonToCustomObject] = "ConvertJsonToCustomObject",
            [CustomObjectToJson] = "ConvertCustomObjectToJson",
        };

    public static bool IsVariablesType(string? type) =>
        type is not null && ModuleActionIds.ContainsKey(type);

    public static string ModuleActionId(string type) =>
        ModuleActionIds.TryGetValue(type, out var id) ? id : type;
}
