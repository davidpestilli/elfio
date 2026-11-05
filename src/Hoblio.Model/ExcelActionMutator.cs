namespace Hoblio.Model;

/// <summary>Clona ações Excel com campos de template/variável atualizados.</summary>
public static class ExcelActionMutator
{
    public static ExcelAction RenameReferences(ExcelAction a, Func<string, string> replaceTemplate, Func<string?, string?> renameVar) =>
        new()
        {
            Operation = a.Operation,
            Instance = renameVar(a.Instance) ?? a.Instance,
            WorksheetName = ReplaceOpt(a.WorksheetName, replaceTemplate),
            WorksheetIndex = ReplaceOpt(a.WorksheetIndex, replaceTemplate),
            Path = ReplaceOpt(a.Path, replaceTemplate),
            Visible = a.Visible,
            ReadOnly = a.ReadOnly,
            DocumentName = ReplaceOpt(a.DocumentName, replaceTemplate),
            StartRow = ReplaceOpt(a.StartRow, replaceTemplate),
            StartColumn = ReplaceOpt(a.StartColumn, replaceTemplate),
            EndRow = ReplaceOpt(a.EndRow, replaceTemplate),
            EndColumn = ReplaceOpt(a.EndColumn, replaceTemplate),
            CellName = ReplaceOpt(a.CellName, replaceTemplate),
            Value = ReplaceOpt(a.Value, replaceTemplate),
            OutputVariable = renameVar(a.OutputVariable),
            OutputVariable2 = renameVar(a.OutputVariable2),
            Index = ReplaceOpt(a.Index, replaceTemplate),
            Macro = ReplaceOpt(a.Macro, replaceTemplate),
            FirstLineIsHeader = a.FirstLineIsHeader,
            SaveOnClose = a.SaveOnClose,
            NewName = ReplaceOpt(a.NewName, replaceTemplate),
            FindText = ReplaceOpt(a.FindText, replaceTemplate),
            ReplaceText = ReplaceOpt(a.ReplaceText, replaceTemplate),
            ColorHex = ReplaceOpt(a.ColorHex, replaceTemplate),
            Mode = ReplaceOpt(a.Mode, replaceTemplate),
            TableName = ReplaceOpt(a.TableName, replaceTemplate),
            TargetInstance = renameVar(a.TargetInstance),
            SourceWorksheetName = ReplaceOpt(a.SourceWorksheetName, replaceTemplate),
            TargetWorksheetName = ReplaceOpt(a.TargetWorksheetName, replaceTemplate),
            WorksheetPosition = ReplaceOpt(a.WorksheetPosition, replaceTemplate),
            AdvancedFilter = a.AdvancedFilter,
            FilterOperator1 = ReplaceOpt(a.FilterOperator1, replaceTemplate),
            FilterValue1 = ReplaceOpt(a.FilterValue1, replaceTemplate),
            FilterOperator2 = ReplaceOpt(a.FilterOperator2, replaceTemplate),
            FilterValue2 = ReplaceOpt(a.FilterValue2, replaceTemplate),
            FilterLogic = ReplaceOpt(a.FilterLogic, replaceTemplate),
            PasteMode = ReplaceOpt(a.PasteMode, replaceTemplate),
            CopyMode = ReplaceOpt(a.CopyMode, replaceTemplate),
            ClearFilterScope = ReplaceOpt(a.ClearFilterScope, replaceTemplate),
            DeleteScope = ReplaceOpt(a.DeleteScope, replaceTemplate),
            SortColumn2 = ReplaceOpt(a.SortColumn2, replaceTemplate),
            SortOrder2 = ReplaceOpt(a.SortOrder2, replaceTemplate),
            MatchCase = a.MatchCase,
            MatchEntireCell = a.MatchEntireCell,
            MatchExact = a.MatchExact,
        };

    static string? ReplaceOpt(string? value, Func<string, string> replace) =>
        value is null ? null : replace(value);

    public static ExcelAction With(ExcelAction a, ExcelActionPatch patch) =>
        new()
        {
            Operation = a.Operation,
            Instance = patch.Instance ?? a.Instance,
            WorksheetName = patch.WorksheetName ?? a.WorksheetName,
            WorksheetIndex = patch.WorksheetIndex ?? a.WorksheetIndex,
            Path = patch.Path ?? a.Path,
            Visible = patch.Visible ?? a.Visible,
            ReadOnly = patch.ReadOnly ?? a.ReadOnly,
            DocumentName = patch.DocumentName ?? a.DocumentName,
            StartRow = patch.StartRow ?? a.StartRow,
            StartColumn = patch.StartColumn ?? a.StartColumn,
            EndRow = patch.EndRow ?? a.EndRow,
            EndColumn = patch.EndColumn ?? a.EndColumn,
            CellName = patch.CellName ?? a.CellName,
            Value = patch.Value ?? a.Value,
            OutputVariable = patch.OutputVariable ?? a.OutputVariable,
            OutputVariable2 = patch.OutputVariable2 ?? a.OutputVariable2,
            Index = patch.Index ?? a.Index,
            Macro = patch.Macro ?? a.Macro,
            FirstLineIsHeader = patch.FirstLineIsHeader ?? a.FirstLineIsHeader,
            SaveOnClose = patch.SaveOnClose ?? a.SaveOnClose,
            NewName = patch.NewName ?? a.NewName,
            FindText = patch.FindText ?? a.FindText,
            ReplaceText = patch.ReplaceText ?? a.ReplaceText,
            ColorHex = patch.ColorHex ?? a.ColorHex,
            Mode = patch.Mode ?? a.Mode,
            TableName = patch.TableName ?? a.TableName,
            TargetInstance = patch.TargetInstance ?? a.TargetInstance,
            SourceWorksheetName = patch.SourceWorksheetName ?? a.SourceWorksheetName,
            TargetWorksheetName = patch.TargetWorksheetName ?? a.TargetWorksheetName,
            WorksheetPosition = patch.WorksheetPosition ?? a.WorksheetPosition,
            AdvancedFilter = patch.AdvancedFilter ?? a.AdvancedFilter,
            FilterOperator1 = patch.FilterOperator1 ?? a.FilterOperator1,
            FilterValue1 = patch.FilterValue1 ?? a.FilterValue1,
            FilterOperator2 = patch.FilterOperator2 ?? a.FilterOperator2,
            FilterValue2 = patch.FilterValue2 ?? a.FilterValue2,
            FilterLogic = patch.FilterLogic ?? a.FilterLogic,
            PasteMode = patch.PasteMode ?? a.PasteMode,
            CopyMode = patch.CopyMode ?? a.CopyMode,
            ClearFilterScope = patch.ClearFilterScope ?? a.ClearFilterScope,
            DeleteScope = patch.DeleteScope ?? a.DeleteScope,
            SortColumn2 = patch.SortColumn2 ?? a.SortColumn2,
            SortOrder2 = patch.SortOrder2 ?? a.SortOrder2,
            MatchCase = patch.MatchCase ?? a.MatchCase,
            MatchEntireCell = patch.MatchEntireCell ?? a.MatchEntireCell,
            MatchExact = patch.MatchExact ?? a.MatchExact,
        };
}

public sealed class ExcelActionPatch
{
    public string? Instance { get; init; }
    public string? WorksheetName { get; init; }
    public string? WorksheetIndex { get; init; }
    public string? Path { get; init; }
    public bool? Visible { get; init; }
    public bool? ReadOnly { get; init; }
    public string? DocumentName { get; init; }
    public string? StartRow { get; init; }
    public string? StartColumn { get; init; }
    public string? EndRow { get; init; }
    public string? EndColumn { get; init; }
    public string? CellName { get; init; }
    public string? Value { get; init; }
    public string? OutputVariable { get; init; }
    public string? OutputVariable2 { get; init; }
    public string? Index { get; init; }
    public string? Macro { get; init; }
    public bool? FirstLineIsHeader { get; init; }
    public bool? SaveOnClose { get; init; }
    public string? NewName { get; init; }
    public string? FindText { get; init; }
    public string? ReplaceText { get; init; }
    public string? ColorHex { get; init; }
    public string? Mode { get; init; }
    public string? TableName { get; init; }
    public string? TargetInstance { get; init; }
    public string? SourceWorksheetName { get; init; }
    public string? TargetWorksheetName { get; init; }
    public string? WorksheetPosition { get; init; }
    public bool? AdvancedFilter { get; init; }
    public string? FilterOperator1 { get; init; }
    public string? FilterValue1 { get; init; }
    public string? FilterOperator2 { get; init; }
    public string? FilterValue2 { get; init; }
    public string? FilterLogic { get; init; }
    public string? PasteMode { get; init; }
    public string? CopyMode { get; init; }
    public string? ClearFilterScope { get; init; }
    public string? DeleteScope { get; init; }
    public string? SortColumn2 { get; init; }
    public string? SortOrder2 { get; init; }
    public bool? MatchCase { get; init; }
    public bool? MatchEntireCell { get; init; }
    public bool? MatchExact { get; init; }
}
