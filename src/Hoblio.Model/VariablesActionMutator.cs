namespace Hoblio.Model;

public static class VariablesActionMutator
{
    public static VariablesAction RenameReferences(
        VariablesAction a,
        Func<string, string> replaceTemplate,
        Func<string?, string?> renameVar) =>
        new()
        {
            Operation = a.Operation,
            DataTableVariable = renameVar(a.DataTableVariable),
            SecondDataTableVariable = renameVar(a.SecondDataTableVariable),
            ListVariable = renameVar(a.ListVariable),
            SecondListVariable = renameVar(a.SecondListVariable),
            OutputVariable = renameVar(a.OutputVariable),
            OutputVariable2 = renameVar(a.OutputVariable2),
            Value = ReplaceOpt(a.Value, replaceTemplate),
            Values = ReplaceOpt(a.Values, replaceTemplate),
            Items = ReplaceOpt(a.Items, replaceTemplate),
            ColumnName = ReplaceOpt(a.ColumnName, replaceTemplate),
            ColumnIndex = ReplaceOpt(a.ColumnIndex, replaceTemplate),
            RowIndex = ReplaceOpt(a.RowIndex, replaceTemplate),
            ColumnSpecifier = a.ColumnSpecifier,
            FindText = ReplaceOpt(a.FindText, replaceTemplate),
            ReplaceText = ReplaceOpt(a.ReplaceText, replaceTemplate),
            FindReplaceMode = a.FindReplaceMode,
            SearchArea = a.SearchArea,
            UseRegex = a.UseRegex,
            Mode = a.Mode,
            SortColumn = ReplaceOpt(a.SortColumn, replaceTemplate),
            SortOrder = a.SortOrder,
            SortColumn2 = ReplaceOpt(a.SortColumn2, replaceTemplate),
            SortOrder2 = a.SortOrder2,
            ColumnWidths = ReplaceOpt(a.ColumnWidths, replaceTemplate),
            FilterColumn = ReplaceOpt(a.FilterColumn, replaceTemplate),
            FilterOperator = a.FilterOperator,
            FilterValue = ReplaceOpt(a.FilterValue, replaceTemplate),
            FilterColumn2 = ReplaceOpt(a.FilterColumn2, replaceTemplate),
            FilterOperator2 = a.FilterOperator2,
            FilterValue2 = ReplaceOpt(a.FilterValue2, replaceTemplate),
            FilterLogic = a.FilterLogic,
            FilterConditions = ReplaceOpt(a.FilterConditions, replaceTemplate),
            JoinColumn = ReplaceOpt(a.JoinColumn, replaceTemplate),
            JoinColumn2 = ReplaceOpt(a.JoinColumn2, replaceTemplate),
            JoinOperation = a.JoinOperation,
            JoinComparisonOperator = a.JoinComparisonOperator,
            Path = ReplaceOpt(a.Path, replaceTemplate),
            Separator = a.Separator,
            CustomSeparator = ReplaceOpt(a.CustomSeparator, replaceTemplate),
            FirstLineIsHeader = a.FirstLineIsHeader,
            MinValue = ReplaceOpt(a.MinValue, replaceTemplate),
            MaxValue = ReplaceOpt(a.MaxValue, replaceTemplate),
            DecimalPlaces = ReplaceOpt(a.DecimalPlaces, replaceTemplate),
            Count = ReplaceOpt(a.Count, replaceTemplate),
            ItemIndex = ReplaceOpt(a.ItemIndex, replaceTemplate),
            PropertyName = ReplaceOpt(a.PropertyName, replaceTemplate),
            IgnoreCase = a.IgnoreCase,
            MatchCase = a.MatchCase,
            MatchEntireCell = a.MatchEntireCell,
            RemoveAllMatches = a.RemoveAllMatches,
            JsonText = ReplaceOpt(a.JsonText, replaceTemplate),
            SourceVariable = renameVar(a.SourceVariable),
        };

    public static VariablesAction With(VariablesAction a, VariablesActionPatch patch) =>
        new()
        {
            Operation = a.Operation,
            DataTableVariable = patch.DataTableVariable ?? a.DataTableVariable,
            SecondDataTableVariable = patch.SecondDataTableVariable ?? a.SecondDataTableVariable,
            ListVariable = patch.ListVariable ?? a.ListVariable,
            SecondListVariable = patch.SecondListVariable ?? a.SecondListVariable,
            OutputVariable = patch.OutputVariable ?? a.OutputVariable,
            OutputVariable2 = patch.OutputVariable2 ?? a.OutputVariable2,
            Value = patch.Value ?? a.Value,
            Values = patch.Values ?? a.Values,
            Items = patch.Items ?? a.Items,
            ColumnName = patch.ColumnName ?? a.ColumnName,
            ColumnIndex = patch.ColumnIndex ?? a.ColumnIndex,
            RowIndex = patch.RowIndex ?? a.RowIndex,
            ColumnSpecifier = patch.ColumnSpecifier ?? a.ColumnSpecifier,
            FindText = patch.FindText ?? a.FindText,
            ReplaceText = patch.ReplaceText ?? a.ReplaceText,
            FindReplaceMode = patch.FindReplaceMode ?? a.FindReplaceMode,
            SearchArea = patch.SearchArea ?? a.SearchArea,
            UseRegex = patch.UseRegex ?? a.UseRegex,
            Mode = patch.Mode ?? a.Mode,
            SortColumn = patch.SortColumn ?? a.SortColumn,
            SortOrder = patch.SortOrder ?? a.SortOrder,
            SortColumn2 = patch.SortColumn2 ?? a.SortColumn2,
            SortOrder2 = patch.SortOrder2 ?? a.SortOrder2,
            ColumnWidths = patch.ColumnWidths ?? a.ColumnWidths,
            FilterColumn = patch.FilterColumn ?? a.FilterColumn,
            FilterOperator = patch.FilterOperator ?? a.FilterOperator,
            FilterValue = patch.FilterValue ?? a.FilterValue,
            FilterColumn2 = patch.FilterColumn2 ?? a.FilterColumn2,
            FilterOperator2 = patch.FilterOperator2 ?? a.FilterOperator2,
            FilterValue2 = patch.FilterValue2 ?? a.FilterValue2,
            FilterLogic = patch.FilterLogic ?? a.FilterLogic,
            FilterConditions = patch.FilterConditions ?? a.FilterConditions,
            JoinColumn = patch.JoinColumn ?? a.JoinColumn,
            JoinColumn2 = patch.JoinColumn2 ?? a.JoinColumn2,
            JoinOperation = patch.JoinOperation ?? a.JoinOperation,
            JoinComparisonOperator = patch.JoinComparisonOperator ?? a.JoinComparisonOperator,
            Path = patch.Path ?? a.Path,
            Separator = patch.Separator ?? a.Separator,
            CustomSeparator = patch.CustomSeparator ?? a.CustomSeparator,
            FirstLineIsHeader = patch.FirstLineIsHeader ?? a.FirstLineIsHeader,
            MinValue = patch.MinValue ?? a.MinValue,
            MaxValue = patch.MaxValue ?? a.MaxValue,
            DecimalPlaces = patch.DecimalPlaces ?? a.DecimalPlaces,
            Count = patch.Count ?? a.Count,
            ItemIndex = patch.ItemIndex ?? a.ItemIndex,
            PropertyName = patch.PropertyName ?? a.PropertyName,
            IgnoreCase = patch.IgnoreCase ?? a.IgnoreCase,
            MatchCase = patch.MatchCase ?? a.MatchCase,
            MatchEntireCell = patch.MatchEntireCell ?? a.MatchEntireCell,
            RemoveAllMatches = patch.RemoveAllMatches ?? a.RemoveAllMatches,
            JsonText = patch.JsonText ?? a.JsonText,
            SourceVariable = patch.SourceVariable ?? a.SourceVariable,
        };

    static string? ReplaceOpt(string? value, Func<string, string> replace) =>
        value is null ? null : replace(value);
}

public sealed class VariablesActionPatch
{
    public string? DataTableVariable { get; init; }
    public string? SecondDataTableVariable { get; init; }
    public string? ListVariable { get; init; }
    public string? SecondListVariable { get; init; }
    public string? OutputVariable { get; init; }
    public string? OutputVariable2 { get; init; }
    public string? Value { get; init; }
    public string? Values { get; init; }
    public string? Items { get; init; }
    public string? ColumnName { get; init; }
    public string? ColumnIndex { get; init; }
    public string? RowIndex { get; init; }
    public string? ColumnSpecifier { get; init; }
    public string? FindText { get; init; }
    public string? ReplaceText { get; init; }
    public string? FindReplaceMode { get; init; }
    public string? SearchArea { get; init; }
    public bool? UseRegex { get; init; }
    public string? Mode { get; init; }
    public string? SortColumn { get; init; }
    public string? SortOrder { get; init; }
    public string? SortColumn2 { get; init; }
    public string? SortOrder2 { get; init; }
    public string? ColumnWidths { get; init; }
    public string? FilterColumn { get; init; }
    public string? FilterOperator { get; init; }
    public string? FilterValue { get; init; }
    public string? FilterColumn2 { get; init; }
    public string? FilterOperator2 { get; init; }
    public string? FilterValue2 { get; init; }
    public string? FilterLogic { get; init; }
    public string? FilterConditions { get; init; }
    public string? JoinColumn { get; init; }
    public string? JoinColumn2 { get; init; }
    public string? JoinOperation { get; init; }
    public string? JoinComparisonOperator { get; init; }
    public string? Path { get; init; }
    public string? Separator { get; init; }
    public string? CustomSeparator { get; init; }
    public bool? FirstLineIsHeader { get; init; }
    public string? MinValue { get; init; }
    public string? MaxValue { get; init; }
    public string? DecimalPlaces { get; init; }
    public string? Count { get; init; }
    public string? ItemIndex { get; init; }
    public string? PropertyName { get; init; }
    public bool? IgnoreCase { get; init; }
    public bool? MatchCase { get; init; }
    public bool? MatchEntireCell { get; init; }
    public bool? RemoveAllMatches { get; init; }
    public string? JsonText { get; init; }
    public string? SourceVariable { get; init; }
}
