namespace Hoblio.Model;

/// <summary>Identificadores JSON (<c>type</c>) e telemetria (<c>action_id</c>) das ações Excel.</summary>
public static class ExcelActionTypes
{
    public const string Launch = "excel_launch";
    public const string Attach = "excel_attach";
    public const string Read = "excel_read";
    public const string GetActiveCell = "excel_get_active_cell";
    public const string Save = "excel_save";
    public const string Write = "excel_write";
    public const string Close = "excel_close";
    public const string SetActiveWorksheet = "excel_set_active_worksheet";
    public const string AddWorksheet = "excel_add_worksheet";
    public const string GetFirstFreeColumnRow = "excel_get_first_free_column_row";
    public const string GetColumnName = "excel_get_column_name";
    public const string ClearCells = "excel_clear_cells";
    public const string SortCells = "excel_sort_cells";
    public const string FilterCells = "excel_filter_cells";
    public const string ClearFilter = "excel_clear_filter";
    public const string GetEmptyCell = "excel_get_empty_cell";

    public const string ResizeColumnsOrRows = "excel_resize_columns_or_rows";
    public const string RunMacro = "excel_run_macro";
    public const string GetActiveWorksheet = "excel_get_active_worksheet";
    public const string GetAllWorksheets = "excel_get_all_worksheets";
    public const string DeleteWorksheet = "excel_delete_worksheet";
    public const string RenameWorksheet = "excel_rename_worksheet";
    public const string CopyWorksheet = "excel_copy_worksheet";
    public const string ActivateCell = "excel_activate_cell";
    public const string SelectCells = "excel_select_cells";
    public const string GetSelectedRange = "excel_get_selected_range";
    public const string CopyCells = "excel_copy_cells";
    public const string PasteCells = "excel_paste_cells";
    public const string DeleteCells = "excel_delete_cells";
    public const string InsertRow = "excel_insert_row";
    public const string DeleteRow = "excel_delete_row";
    public const string InsertColumn = "excel_insert_column";
    public const string DeleteColumn = "excel_delete_column";
    public const string FindReplace = "excel_find_replace";
    public const string GetFirstFreeRowOnColumn = "excel_get_first_free_row_on_column";
    public const string ReadCellFormula = "excel_read_cell_formula";
    public const string GetTableRange = "excel_get_table_range";
    public const string AutofillCells = "excel_autofill_cells";
    public const string AppendCells = "excel_append_cells";
    public const string LookupRange = "excel_lookup_range";
    public const string SetColor = "excel_set_color";

    public static readonly IReadOnlyList<string> All =
    [
        Launch, Attach, Read, GetActiveCell, Save, Write, Close, SetActiveWorksheet, AddWorksheet,
        GetFirstFreeColumnRow, GetColumnName, ClearCells, SortCells, FilterCells, ClearFilter, GetEmptyCell,
        ResizeColumnsOrRows, RunMacro, GetActiveWorksheet, GetAllWorksheets, DeleteWorksheet, RenameWorksheet,
        CopyWorksheet, ActivateCell, SelectCells, GetSelectedRange, CopyCells, PasteCells, DeleteCells,
        InsertRow, DeleteRow, InsertColumn, DeleteColumn, FindReplace, GetFirstFreeRowOnColumn, ReadCellFormula,
        GetTableRange, AutofillCells, AppendCells, LookupRange, SetColor,
    ];

    public static readonly IReadOnlyDictionary<string, string> ModuleActionIds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Launch] = "LaunchExcel",
            [Attach] = "AttachToRunningExcel",
            [Read] = "ReadFromExcel",
            [GetActiveCell] = "GetActiveCell",
            [Save] = "SaveExcel",
            [Write] = "WriteToExcel",
            [Close] = "CloseExcel",
            [SetActiveWorksheet] = "SetActiveWorksheet",
            [AddWorksheet] = "AddWorksheet",
            [GetFirstFreeColumnRow] = "GetFirstFreeColumnRow",
            [GetColumnName] = "GetColumnName",
            [ClearCells] = "ClearCellsFromExcel",
            [SortCells] = "SortCellsFromExcel",
            [FilterCells] = "FilterCellsFromExcel",
            [ClearFilter] = "ClearFilterFromExcel",
            [GetEmptyCell] = "GetEmptyCellFromExcel",
            [ResizeColumnsOrRows] = "ResizeColumnsOrRows",
            [RunMacro] = "RunExcelMacro",
            [GetActiveWorksheet] = "GetActiveWorksheet",
            [GetAllWorksheets] = "GetAllWorksheets",
            [DeleteWorksheet] = "DeleteWorksheet",
            [RenameWorksheet] = "RenameWorksheet",
            [CopyWorksheet] = "CopyExcelWorksheet",
            [ActivateCell] = "ActivateCellInExcel",
            [SelectCells] = "SelectCellsFromExcel",
            [GetSelectedRange] = "GetSelectedCellRange",
            [CopyCells] = "CopyCellsFromExcel",
            [PasteCells] = "PasteCellsToExcel",
            [DeleteCells] = "DeleteCells",
            [InsertRow] = "InsertRow",
            [DeleteRow] = "DeleteRow",
            [InsertColumn] = "InsertColumn",
            [DeleteColumn] = "DeleteColumn",
            [FindReplace] = "FindAndReplace",
            [GetFirstFreeRowOnColumn] = "GetFirstFreeRowOnColumn",
            [ReadCellFormula] = "ReadCellFormula",
            [GetTableRange] = "GetTableRange",
            [AutofillCells] = "AutoFillCells",
            [AppendCells] = "AppendCells",
            [LookupRange] = "LookupRange",
            [SetColor] = "SetColor",
        };

    public static bool IsExcelType(string? type) =>
        type is not null && ModuleActionIds.ContainsKey(type);

    public static string ModuleActionId(string type) =>
        ModuleActionIds.TryGetValue(type, out var id) ? id : type;
}
