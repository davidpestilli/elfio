using System.Globalization;
using System.Text.Json;
using Hoblio.Model;

namespace Hoblio.Runtime;

public static class VariablesContext
{
    public static string Execute(VariablesAction action, RunVariables vars, string? projectRoot)
    {
        return action.Operation switch
        {
            VariablesActionTypes.CreateDatatable => CreateDatatable(action, vars),
            VariablesActionTypes.AddRow => MutateTable(action, vars, AddRow),
            VariablesActionTypes.DeleteRow => MutateTable(action, vars, DeleteRow),
            VariablesActionTypes.ModifyCell => MutateTable(action, vars, ModifyCell),
            VariablesActionTypes.FindReplaceDatatable => FindReplace(action, vars),
            VariablesActionTypes.AddColumn => MutateTable(action, vars, AddColumn),
            VariablesActionTypes.DeleteColumn => MutateTable(action, vars, DeleteColumn),
            VariablesActionTypes.DeleteEmptyRows => MutateTable(action, vars, t => t.RemoveEmptyRows()),
            VariablesActionTypes.DeleteDuplicateRows => MutateTable(action, vars, DeleteDuplicateRows),
            VariablesActionTypes.ClearDatatable => MutateTable(action, vars, t => t.ClearRows()),
            VariablesActionTypes.SortDatatable => MutateTable(action, vars, SortTable),
            VariablesActionTypes.FilterDatatable => FilterTable(action, vars),
            VariablesActionTypes.MergeDatatables => MergeTables(action, vars),
            VariablesActionTypes.JoinDatatable => JoinTables(action, vars),
            VariablesActionTypes.ReadCsv => ReadCsv(action, vars, projectRoot),
            VariablesActionTypes.WriteCsv => WriteCsv(action, vars, projectRoot),
            VariablesActionTypes.ColumnToList => ColumnToList(action, vars),
            VariablesActionTypes.TruncateNumber => TruncateNumber(action, vars),
            VariablesActionTypes.RandomNumber => RandomNumber(action, vars),
            VariablesActionTypes.CreateList => CreateList(action, vars),
            VariablesActionTypes.RemoveListItem => ListActions.RemoveItem(action, vars),
            VariablesActionTypes.SortList => ListActions.SortList(action, vars),
            VariablesActionTypes.ShuffleList => ListActions.ShuffleList(action, vars),
            VariablesActionTypes.MergeLists => ListActions.MergeLists(action, vars),
            VariablesActionTypes.ReverseList => ListActions.ReverseList(action, vars),
            VariablesActionTypes.RemoveDuplicateListItems => ListActions.RemoveDuplicates(action, vars),
            VariablesActionTypes.FindCommonListItems => ListActions.FindCommon(action, vars),
            VariablesActionTypes.SubtractLists => ListActions.SubtractLists(action, vars),
            VariablesActionTypes.FilterList => ListActions.FilterList(action, vars),
            VariablesActionTypes.JsonToCustomObject => JsonToObject(action, vars),
            VariablesActionTypes.CustomObjectToJson => ObjectToJson(action, vars),
            _ => throw new InvalidOperationException($"unknown variables action `{action.Operation}`"),
        };
    }

    static string CreateDatatable(VariablesAction action, RunVariables vars)
    {
        var name = RequireVarName(action.DataTableVariable, "data_table_variable");
        var table = FlowTable.Empty();
        if (!string.IsNullOrWhiteSpace(action.Values))
        {
            var cols = FlowTable.ParseRowValues(action.Values, vars);
            table.Columns.AddRange(cols);
        }
        else if (!string.IsNullOrWhiteSpace(action.Value))
        {
            foreach (var part in RunVariables.Interpolate(action.Value, vars).Split(',', StringSplitOptions.TrimEntries))
                table.Columns.Add(part);
        }
        vars[name] = table.ToJson();
        return $"{action.Operation}: `{name}` ({table.Columns.Count} columns)";
    }

    static string MutateTable(VariablesAction action, RunVariables vars, Action<FlowTable, VariablesAction, RunVariables> mutate)
    {
        var name = RequireVarName(action.DataTableVariable, "data_table_variable");
        var table = FlowTable.Parse(vars[name]);
        mutate(table, action, vars);
        vars[name] = table.ToJson();
        return $"{action.Operation}: `{name}`";
    }

    static string MutateTable(VariablesAction action, RunVariables vars, Action<FlowTable> mutate)
    {
        var name = RequireVarName(action.DataTableVariable, "data_table_variable");
        var table = FlowTable.Parse(vars[name]);
        mutate(table);
        vars[name] = table.ToJson();
        return $"{action.Operation}: `{name}`";
    }

    static void AddRow(FlowTable table, VariablesAction action, RunVariables vars)
    {
        var values = FlowTable.ParseRowValues(action.Values ?? action.Value, vars);
        var mode = (action.Mode ?? VariablesEnums.AddRowMode.Append).Trim().ToLowerInvariant();
        var index = mode == VariablesEnums.AddRowMode.Insert
            ? FlowTable.ParseRowIndex(action.RowIndex, table.Rows.Count, allowAppend: true)
            : table.Rows.Count;
        table.InsertRow(index, values);
    }

    static void DeleteRow(FlowTable table, VariablesAction action, RunVariables vars)
    {
        var idx = FlowTable.ParseRowIndex(action.RowIndex, table.Rows.Count);
        table.DeleteRowAt(idx);
    }

    static void ModifyCell(FlowTable table, VariablesAction action, RunVariables vars)
    {
        var row = FlowTable.ParseRowIndex(action.RowIndex, table.Rows.Count);
        var col = table.ResolveColumnIndex(action.ColumnName, action.ColumnIndex);
        table.SetCell(row, col, RunVariables.Interpolate(action.Value ?? "", vars));
    }

    static void AddColumn(FlowTable table, VariablesAction action, RunVariables vars)
    {
        var name = RunVariables.Interpolate(action.ColumnName ?? action.Value ?? "Column", vars);
        var mode = (action.Mode ?? VariablesEnums.AddColumnMode.Append).Trim().ToLowerInvariant();
        var index = mode == VariablesEnums.AddColumnMode.Insert
            ? table.ResolveColumnIndex(null, action.ColumnIndex)
            : table.Columns.Count;
        var defaultVal = RunVariables.Interpolate(action.Values ?? "", vars);
        table.InsertColumn(index, name, defaultVal);
    }

    static void DeleteColumn(FlowTable table, VariablesAction action, RunVariables vars)
    {
        var spec = (action.ColumnSpecifier ?? VariablesEnums.ColumnSpecifier.ByName).Trim().ToLowerInvariant();
        var idx = spec == VariablesEnums.ColumnSpecifier.ByIndex
            ? table.ResolveColumnIndex(null, action.ColumnIndex)
            : table.ResolveColumnIndex(action.ColumnName, null);
        table.DeleteColumnAt(idx);
    }

    static void SortTable(FlowTable table, VariablesAction action, RunVariables vars)
    {
        var keys = new List<(int Column, bool Descending)>();
        var col1 = table.ResolveColumnIndex(action.SortColumn ?? action.ColumnName, action.ColumnIndex);
        keys.Add((col1, VariablesSemantics.SortDescending(action.SortOrder)));
        if (!string.IsNullOrWhiteSpace(action.SortColumn2))
        {
            var col2 = table.ResolveColumnIndex(action.SortColumn2, null);
            keys.Add((col2, VariablesSemantics.SortDescending(action.SortOrder2)));
        }
        table.SortByColumns(keys);
    }

    static void DeleteDuplicateRows(FlowTable table, VariablesAction action, RunVariables vars)
    {
        if (string.IsNullOrWhiteSpace(action.ColumnName) && string.IsNullOrWhiteSpace(action.ColumnIndex))
        {
            table.RemoveDuplicateRows();
            return;
        }
        var indexes = new List<int>();
        foreach (var part in (action.ColumnName ?? action.ColumnIndex ?? "").Split(',', StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx))
                indexes.Add(idx);
            else
                indexes.Add(table.ResolveColumnIndex(part, null));
        }
        table.RemoveDuplicateRows(indexes);
    }

    static string FindReplace(VariablesAction action, RunVariables vars)
    {
        var name = RequireVarName(action.DataTableVariable, "data_table_variable");
        var table = FlowTable.Parse(vars[name]);
        var count = table.FindReplace(action, vars);
        vars[name] = table.ToJson();
        if (!string.IsNullOrWhiteSpace(action.OutputVariable))
            vars[action.OutputVariable.Trim()] = count.ToString(CultureInfo.InvariantCulture);
        return $"{action.Operation}: `{name}` matches={count}";
    }

    static string FilterTable(VariablesAction action, RunVariables vars)
    {
        var srcName = RequireVarName(action.DataTableVariable, "data_table_variable");
        var src = FlowTable.Parse(vars[srcName]);
        var filtered = FlowTable.Empty();
        filtered.Columns.AddRange(src.Columns);
        foreach (var row in src.Rows)
        {
            if (src.RowMatchesFilter(row, action, vars))
                filtered.Rows.Add(new List<string>(row));
        }
        var outName = action.OutputVariable?.Trim();
        if (string.IsNullOrEmpty(outName))
            outName = srcName;
        vars[outName] = filtered.ToJson();
        return $"{action.Operation}: `{outName}` rows={filtered.Rows.Count}";
    }

    static string MergeTables(VariablesAction action, RunVariables vars)
    {
        var aName = RequireVarName(action.DataTableVariable, "data_table_variable");
        var bName = RequireVarName(action.SecondDataTableVariable, "second_data_table_variable");
        var merged = FlowTable.Merge(
            FlowTable.Parse(vars[aName]),
            FlowTable.Parse(vars[bName]),
            action.Mode ?? VariablesEnums.MergeMode.IgnoreExtraColumns);
        var outName = action.OutputVariable?.Trim();
        if (string.IsNullOrEmpty(outName))
            outName = aName;
        vars[outName] = merged.ToJson();
        return $"{action.Operation}: `{outName}` rows={merged.Rows.Count}";
    }

    static string JoinTables(VariablesAction action, RunVariables vars)
    {
        var aName = RequireVarName(action.DataTableVariable, "data_table_variable");
        var bName = RequireVarName(action.SecondDataTableVariable, "second_data_table_variable");
        var joinCol = RunVariables.Interpolate(action.JoinColumn ?? "", vars);
        if (string.IsNullOrWhiteSpace(joinCol))
            throw new InvalidOperationException("join_column required");
        var joined = FlowTable.Join(
            FlowTable.Parse(vars[aName]),
            FlowTable.Parse(vars[bName]),
            joinCol,
            string.IsNullOrWhiteSpace(action.JoinColumn2) ? null : RunVariables.Interpolate(action.JoinColumn2, vars),
            action.JoinOperation ?? VariablesEnums.JoinOperation.Inner,
            action.JoinComparisonOperator);
        var outName = action.OutputVariable?.Trim();
        if (string.IsNullOrEmpty(outName))
            outName = aName;
        vars[outName] = joined.ToJson();
        return $"{action.Operation}: `{outName}` rows={joined.Rows.Count}";
    }

    static string ReadCsv(VariablesAction action, RunVariables vars, string? projectRoot)
    {
        var path = FileActions.ResolveProjectPath(RunVariables.Interpolate(action.Path ?? "", vars), projectRoot);
        var text = File.ReadAllText(path);
        var readMode = VariablesSemantics.NormalizeCsvReadMode(action.Mode);
        FlowTable table;
        if (readMode == VariablesEnums.CsvReadMode.FixedWidth)
        {
            var widths = FlowTable.ParseColumnWidths(action.ColumnWidths, vars);
            table = FlowTable.FromFixedWidth(text, widths, action.FirstLineIsHeader ?? true);
        }
        else
        {
            var sep = ResolveSeparator(action);
            table = FlowTable.FromCsv(text, sep, action.FirstLineIsHeader ?? true);
        }
        var name = RequireVarName(action.DataTableVariable ?? action.OutputVariable, "data_table_variable");
        vars[name] = table.ToJson();
        return $"{action.Operation}: `{name}` rows={table.Rows.Count}";
    }

    static string WriteCsv(VariablesAction action, RunVariables vars, string? projectRoot)
    {
        var name = RequireVarName(action.DataTableVariable, "data_table_variable");
        var table = FlowTable.Parse(vars[name]);
        var csv = FlowTable.ToCsv(table, ResolveSeparatorString(action));
        var path = FileActions.ResolveProjectPath(RunVariables.Interpolate(action.Path ?? "", vars), projectRoot);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, csv);
        if (!string.IsNullOrWhiteSpace(action.OutputVariable))
            vars[action.OutputVariable.Trim()] = csv;
        return $"{action.Operation}: `{path}`";
    }

    static char ResolveSeparator(VariablesAction action)
    {
        var sep = (action.Separator ?? VariablesEnums.CsvSeparator.Comma).Trim().ToLowerInvariant();
        return sep switch
        {
            VariablesEnums.CsvSeparator.Semicolon => ';',
            VariablesEnums.CsvSeparator.Tab => '\t',
            VariablesEnums.CsvSeparator.Custom => (action.CustomSeparator ?? ",")[0],
            _ => ',',
        };
    }

    static string ResolveSeparatorString(VariablesAction action) =>
        ResolveSeparator(action).ToString();

    static string ColumnToList(VariablesAction action, RunVariables vars)
    {
        var tableName = RequireVarName(action.DataTableVariable, "data_table_variable");
        var table = FlowTable.Parse(vars[tableName]);
        var col = table.ResolveColumnIndex(action.ColumnName, action.ColumnIndex);
        var list = table.ColumnValues(col);
        var listVar = RequireVarName(action.ListVariable ?? action.OutputVariable, "list_variable");
        vars[listVar] = JsonSerializer.Serialize(list);
        return $"{action.Operation}: `{listVar}` ({list.Count} items)";
    }

    static string TruncateNumber(VariablesAction action, RunVariables vars)
    {
        var outVar = RequireVarName(action.OutputVariable, "output_variable");
        var source = action.SourceVariable?.Trim();
        var raw = !string.IsNullOrEmpty(source) && vars.TryGetValue(source, out var v)
            ? v
            : RunVariables.Interpolate(action.Value ?? "", vars);
        var n = ExpressionEvaluator.ParseNumber(raw);
        var op = VariablesSemantics.NormalizeTruncateOperation(action.Mode);
        var dpText = action.DecimalPlaces is null ? "0" : RunVariables.Interpolate(action.DecimalPlaces, vars);
        var dp = int.Parse(dpText.Trim(), CultureInfo.InvariantCulture);
        var result = op switch
        {
            VariablesEnums.TruncateOperation.IntegerPart => Math.Truncate(n),
            VariablesEnums.TruncateOperation.DecimalPart => n - Math.Truncate(n),
            VariablesEnums.TruncateOperation.Truncate => TruncateToDecimals(n, dp),
            _ => Math.Round(n, dp, MidpointRounding.AwayFromZero),
        };
        vars[outVar] = FormatNumber(result);
        return $"{action.Operation}: `{outVar}` = {vars[outVar]}";
    }

    static double TruncateToDecimals(double n, int dp)
    {
        if (dp <= 0)
            return Math.Truncate(n);
        var factor = Math.Pow(10, dp);
        return Math.Truncate(n * factor) / factor;
    }

    static string RandomNumber(VariablesAction action, RunVariables vars)
    {
        var min = ExpressionEvaluator.ParseNumber(RunVariables.Interpolate(action.MinValue ?? "0", vars));
        var max = ExpressionEvaluator.ParseNumber(RunVariables.Interpolate(action.MaxValue ?? "100", vars));
        if (max < min)
            (min, max) = (max, min);
        var mode = (action.Mode ?? VariablesEnums.RandomNumberMode.Single).Trim().ToLowerInvariant();
        var rng = Random.Shared;
        var asInteger = mode is VariablesEnums.RandomNumberMode.Integer or VariablesEnums.RandomNumberMode.IntegerList;
        if (mode is VariablesEnums.RandomNumberMode.List or VariablesEnums.RandomNumberMode.IntegerList)
        {
            var countText = RunVariables.Interpolate(action.Count ?? "5", vars);
            var count = int.Parse(countText.Trim(), CultureInfo.InvariantCulture);
            var list = new List<string>();
            for (var i = 0; i < count; i++)
                list.Add(FormatRandom(rng, min, max, asInteger));
            var outVar = RequireVarName(action.OutputVariable ?? action.ListVariable, "output_variable");
            vars[outVar] = JsonSerializer.Serialize(list);
            return $"{action.Operation}: `{outVar}` ({list.Count} values)";
        }
        var singleOut = RequireVarName(action.OutputVariable, "output_variable");
        vars[singleOut] = FormatRandom(rng, min, max, asInteger);
        return $"{action.Operation}: `{singleOut}` = {vars[singleOut]}";
    }

    static string CreateList(VariablesAction action, RunVariables vars)
    {
        var name = RequireVarName(action.ListVariable, "list_variable");
        var items = ParseListItems(action.Items, vars);
        vars[name] = JsonSerializer.Serialize(items);
        return $"{action.Operation}: `{name}` ({items.Count} items)";
    }

    static List<string> ParseListItems(string? template, RunVariables vars)
    {
        var raw = RunVariables.Interpolate(template ?? "", vars).Trim();
        if (string.IsNullOrEmpty(raw))
            return [];
        if (raw.StartsWith('['))
            return ExpressionEvaluator.ParseStringList(raw);
        return raw.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    static string JsonToObject(VariablesAction action, RunVariables vars)
    {
        var outVar = RequireVarName(action.OutputVariable, "output_variable");
        var json = "";
        if (action.SourceVariable is { } srcVar && vars.TryGetValue(srcVar.Trim(), out var fromVar))
            json = fromVar;
        if (string.IsNullOrWhiteSpace(json))
            json = RunVariables.InterpolateUnlessJsonLiteral(action.JsonText ?? action.Value ?? "", vars);
        using var doc = JsonDocument.Parse(json);
        vars[outVar] = doc.RootElement.GetRawText();
        return $"{action.Operation}: `{outVar}`";
    }

    static string ObjectToJson(VariablesAction action, RunVariables vars)
    {
        var outVar = RequireVarName(action.OutputVariable, "output_variable");
        var src = RequireVarName(action.SourceVariable, "source_variable");
        if (!vars.TryGetValue(src, out var raw))
            throw new InvalidOperationException($"variable `{src}` is not set");
        using var doc = JsonDocument.Parse(raw);
        vars[outVar] = JsonSerializer.Serialize(doc.RootElement);
        return $"{action.Operation}: `{outVar}`";
    }

    static string RequireVarName(string? name, string field)
    {
        var trimmed = name?.Trim() ?? "";
        if (string.IsNullOrEmpty(trimmed))
            throw new InvalidOperationException($"{field} is required");
        return trimmed;
    }

    static string FormatRandom(Random rng, double min, double max, bool asInteger)
    {
        if (asInteger)
        {
            var imin = (int)Math.Ceiling(min);
            var imax = (int)Math.Floor(max);
            if (imax < imin)
                (imin, imax) = (imax, imin);
            return rng.Next(imin, imax + 1).ToString(CultureInfo.InvariantCulture);
        }
        return FormatNumber(min + rng.NextDouble() * (max - min));
    }

    static string FormatNumber(double n)
    {
        var rounded = Math.Round(n);
        if (Math.Abs(n - rounded) < 1e-9)
            return ((long)rounded).ToString(CultureInfo.InvariantCulture);
        return n.ToString(CultureInfo.InvariantCulture);
    }
}
