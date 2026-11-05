using Hoblio.Model;

namespace Hoblio.Studio;

static class ExcelActionDefaults
{
    public static ExcelAction? TryCreate(string type, IReadOnlySet<string>? takenNames)
    {
        if (!ExcelActionTypes.IsExcelType(type))
            return null;

        takenNames ??= new HashSet<string>();
        var instance = NextName("ExcelInstance", takenNames) ?? "ExcelInstance";
        var cellOut = NextName("ExcelValue", takenNames) ?? "ExcelValue1";

        return type switch
        {
            ExcelActionTypes.Launch => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                Visible = true,
                Path = "",
            },
            ExcelActionTypes.Attach => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                DocumentName = "",
            },
            ExcelActionTypes.Read => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartRow = "1",
                StartColumn = "A",
                OutputVariable = cellOut,
            },
            ExcelActionTypes.GetActiveCell => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                OutputVariable = cellOut,
            },
            ExcelActionTypes.Write => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartRow = "1",
                StartColumn = "A",
                Value = "",
            },
            ExcelActionTypes.Save => new ExcelAction { Operation = type, Instance = instance },
            ExcelActionTypes.Close => new ExcelAction { Operation = type, Instance = instance, SaveOnClose = false },
            ExcelActionTypes.SetActiveWorksheet => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                WorksheetName = "Sheet1",
            },
            ExcelActionTypes.AddWorksheet => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                NewName = "Planilha2",
            },
            ExcelActionTypes.GetFirstFreeColumnRow => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                OutputVariable = NextName("FirstFreeRow", takenNames) ?? "FirstFreeRow1",
                OutputVariable2 = NextName("FirstFreeColumn", takenNames) ?? "FirstFreeColumn1",
            },
            ExcelActionTypes.GetColumnName => new ExcelAction
            {
                Operation = type,
                Index = "1",
                OutputVariable = NextName("ColumnName", takenNames) ?? "ColumnName1",
            },
            ExcelActionTypes.RunMacro => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                Macro = "Macro1",
            },
            ExcelActionTypes.GetActiveWorksheet => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                OutputVariable = NextName("ActiveSheet", takenNames) ?? "ActiveSheet1",
            },
            ExcelActionTypes.GetAllWorksheets => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                OutputVariable = NextName("WorksheetList", takenNames) ?? "WorksheetList1",
            },
            ExcelActionTypes.InsertRow or ExcelActionTypes.DeleteRow
                or ExcelActionTypes.InsertColumn or ExcelActionTypes.DeleteColumn => new ExcelAction
                {
                    Operation = type,
                    Instance = instance,
                    Index = "1",
                },
            ExcelActionTypes.FindReplace => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                FindText = "",
                ReplaceText = "",
            },
            ExcelActionTypes.SetColor => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartRow = "1",
                StartColumn = "A",
                ColorHex = "#FFFF00",
            },
            ExcelActionTypes.SortCells => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartColumn = "A",
                FirstLineIsHeader = true,
            },
            ExcelActionTypes.FilterCells => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartColumn = "A",
                FilterOperator1 = ExcelEnums.FilterOperator.Equal,
                FilterValue1 = "",
                FilterLogic = ExcelEnums.FilterLogic.And,
            },
            ExcelActionTypes.CopyWorksheet => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                SourceWorksheetName = "Planilha1",
                TargetWorksheetName = "Planilha1 (cópia)",
                WorksheetPosition = ExcelEnums.WorksheetPosition.After,
            },
            ExcelActionTypes.PasteCells => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartRow = "1",
                StartColumn = "A",
                PasteMode = ExcelEnums.PasteMode.All,
            },
            ExcelActionTypes.DeleteCells => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartRow = "1",
                StartColumn = "A",
                DeleteScope = ExcelEnums.DeleteScope.Cells,
                Mode = "left",
            },
            ExcelActionTypes.ClearFilter => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                ClearFilterScope = ExcelEnums.ClearFilterScope.All,
            },
            ExcelActionTypes.LookupRange => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartRow = "1",
                StartColumn = "A",
                EndRow = "10",
                EndColumn = "C",
                Index = "2",
                Mode = "vlookup",
                OutputVariable = cellOut,
            },
            ExcelActionTypes.GetTableRange => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                TableName = "Tabela1",
                OutputVariable = NextName("TableRange", takenNames) ?? "TableRange1",
            },
            ExcelActionTypes.AppendCells => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartColumn = "A",
                Value = "",
            },
            ExcelActionTypes.AutofillCells => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartRow = "1",
                StartColumn = "A",
                EndRow = "10",
                EndColumn = "A",
            },
            ExcelActionTypes.DeleteWorksheet => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                WorksheetName = "Planilha1",
            },
            ExcelActionTypes.RenameWorksheet => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                WorksheetName = "Planilha1",
                NewName = "Renomeada",
            },
            ExcelActionTypes.GetFirstFreeRowOnColumn => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartColumn = "A",
                OutputVariable = NextName("FreeRow", takenNames) ?? "FreeRow1",
            },
            ExcelActionTypes.ReadCellFormula => new ExcelAction
            {
                Operation = type,
                Instance = instance,
                StartRow = "1",
                StartColumn = "A",
                OutputVariable = NextName("Formula", takenNames) ?? "Formula1",
            },
            _ => new ExcelAction { Operation = type, Instance = instance },
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
