using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hoblio.Model;

namespace Hoblio.Runtime;

/// <summary>Tabela de dados em memória (JSON: columns + rows).</summary>
public sealed class FlowTable
{
    public List<string> Columns { get; } = [];
    public List<List<string>> Rows { get; } = [];

    public static FlowTable Empty() => new();

    public static FlowTable WithColumns(IEnumerable<string> columns)
    {
        var t = new FlowTable();
        t.Columns.AddRange(columns);
        return t;
    }

    public string ToJson() =>
        JsonSerializer.Serialize(new { columns = Columns, rows = Rows });

    /// <summary>Converte saída legada de <c>browser_extract_table</c> (array de linhas) para JSON canônico.</summary>
    public static string NormalizeStoredTableJson(string raw, bool firstRowIsHeader = true)
    {
        var trimmed = raw.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Empty().ToJson();
        if (trimmed.StartsWith('{') && trimmed.Contains("\"columns\"", StringComparison.Ordinal))
            return trimmed;
        return Parse(trimmed, firstRowIsHeader).ToJson();
    }

    public static FlowTable Parse(string raw, bool firstRowIsHeader = false)
    {
        var trimmed = raw.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Empty();

        using var doc = JsonDocument.Parse(trimmed);
        if (doc.RootElement.ValueKind == JsonValueKind.Object
            && doc.RootElement.TryGetProperty("columns", out var colsEl)
            && doc.RootElement.TryGetProperty("rows", out var rowsEl))
        {
            var table = new FlowTable();
            foreach (var c in colsEl.EnumerateArray())
                table.Columns.Add(c.GetString() ?? "");
            foreach (var rowEl in rowsEl.EnumerateArray())
            {
                var row = new List<string>();
                foreach (var cell in rowEl.EnumerateArray())
                    row.Add(CellToString(cell));
                table.NormalizeRowWidth(row);
                table.Rows.Add(row);
            }
            return table;
        }

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            var table = new FlowTable();
            var rowIndex = 0;
            foreach (var rowEl in doc.RootElement.EnumerateArray())
            {
                if (rowEl.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException("data table array must contain row arrays");
                var row = new List<string>();
                foreach (var cell in rowEl.EnumerateArray())
                    row.Add(CellToString(cell));
                if (rowIndex == 0 && firstRowIsHeader)
                {
                    table.Columns.AddRange(row);
                }
                else
                {
                    if (table.Columns.Count == 0)
                    {
                        for (var i = 0; i < row.Count; i++)
                            table.Columns.Add($"Column{i + 1}");
                    }
                    table.NormalizeRowWidth(row);
                    table.Rows.Add(row);
                }
                rowIndex++;
            }
            return table;
        }

        throw new InvalidOperationException("data table variable must be JSON object {columns,rows} or array of rows");
    }

    static string CellToString(JsonElement cell) =>
        cell.ValueKind switch
        {
            JsonValueKind.String => cell.GetString() ?? "",
            JsonValueKind.Number => cell.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => cell.GetRawText(),
        };

    void NormalizeRowWidth(List<string> row)
    {
        while (row.Count < Columns.Count)
            row.Add("");
        while (row.Count > Columns.Count && Columns.Count > 0)
            row.RemoveAt(row.Count - 1);
        if (Columns.Count == 0 && row.Count > 0)
        {
            for (var i = 0; i < row.Count; i++)
                Columns.Add($"Column{i + 1}");
        }
    }

    public int ResolveColumnIndex(string? name, string? indexText)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var idx = Columns.FindIndex(c => string.Equals(c, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                throw new InvalidOperationException($"column `{name}` not found");
            return idx;
        }
        if (!string.IsNullOrWhiteSpace(indexText))
        {
            if (!int.TryParse(indexText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx))
                throw new InvalidOperationException($"invalid column index `{indexText}`");
            if (idx < 0 || idx >= Columns.Count)
                throw new InvalidOperationException($"column index {idx} out of range");
            return idx;
        }
        throw new InvalidOperationException("column_name or column_index required");
    }

    public static int ParseRowIndex(string? indexText, int rowCount, bool allowAppend = false)
    {
        if (string.IsNullOrWhiteSpace(indexText))
        {
            if (allowAppend)
                return rowCount;
            throw new InvalidOperationException("row_index required");
        }
        if (!int.TryParse(indexText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx))
            throw new InvalidOperationException($"invalid row index `{indexText}`");
        if (idx < 0 || idx > rowCount || (!allowAppend && idx >= rowCount))
            throw new InvalidOperationException($"row index {idx} out of range");
        return idx;
    }

    public void SetCell(int row, int col, string value)
    {
        while (Rows.Count <= row)
            Rows.Add(new List<string>(new string[Columns.Count]));
        var r = Rows[row];
        while (r.Count < Columns.Count)
            r.Add("");
        r[col] = value;
    }

    public string GetCell(int row, int col)
    {
        if (row < 0 || row >= Rows.Count)
            return "";
        var r = Rows[row];
        if (col < 0 || col >= r.Count)
            return "";
        return r[col];
    }

    public List<string> ColumnValues(int col)
    {
        var list = new List<string>();
        foreach (var row in Rows)
            list.Add(col < row.Count ? row[col] : "");
        return list;
    }

    public void DeleteRowAt(int index)
    {
        if (index < 0 || index >= Rows.Count)
            throw new InvalidOperationException($"row index {index} out of range");
        Rows.RemoveAt(index);
    }

    public void DeleteColumnAt(int index)
    {
        if (index < 0 || index >= Columns.Count)
            throw new InvalidOperationException($"column index {index} out of range");
        Columns.RemoveAt(index);
        foreach (var row in Rows)
        {
            if (index < row.Count)
                row.RemoveAt(index);
        }
    }

    public void InsertColumn(int index, string name, string? defaultValue = "")
    {
        if (index < 0 || index > Columns.Count)
            throw new InvalidOperationException($"column index {index} out of range");
        Columns.Insert(index, name);
        foreach (var row in Rows)
            row.Insert(index, defaultValue ?? "");
    }

    public void InsertRow(int index, IReadOnlyList<string> values)
    {
        if (index < 0 || index > Rows.Count)
            throw new InvalidOperationException($"row index {index} out of range");
        var row = new List<string>();
        for (var i = 0; i < Columns.Count; i++)
            row.Add(i < values.Count ? values[i] : "");
        Rows.Insert(index, row);
    }

    public void ClearRows() => Rows.Clear();

    public void RemoveEmptyRows()
    {
        Rows.RemoveAll(row => row.All(string.IsNullOrWhiteSpace));
    }

    public void RemoveDuplicateRows(IReadOnlyList<int>? columnIndexes = null)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = Rows.Count - 1; i >= 0; i--)
        {
            var key = columnIndexes is { Count: > 0 }
                ? string.Join('\u001f', columnIndexes.Select(ci => ci < Rows[i].Count ? Rows[i][ci] : ""))
                : string.Join('\u001f', Rows[i]);
            if (!seen.Add(key))
                Rows.RemoveAt(i);
        }
    }

    public void SortByColumns(IReadOnlyList<(int Column, bool Descending)> keys)
    {
        if (keys.Count == 0)
            return;
        Rows.Sort((a, b) =>
        {
            foreach (var (col, descending) in keys)
            {
                var cmp = CompareCell(a, b, col, descending);
                if (cmp != 0)
                    return cmp;
            }
            return 0;
        });
    }

    static int CompareCell(List<string> a, List<string> b, int col, bool descending)
    {
        var av = col < a.Count ? a[col] : "";
        var bv = col < b.Count ? b[col] : "";
        int core;
        if (double.TryParse(av, NumberStyles.Float, CultureInfo.InvariantCulture, out var an)
            && double.TryParse(bv, NumberStyles.Float, CultureInfo.InvariantCulture, out var bn))
            core = an.CompareTo(bn);
        else
            core = string.Compare(av, bv, StringComparison.OrdinalIgnoreCase);
        return descending ? -core : core;
    }

    public bool RowMatchesFilter(List<string> row, VariablesAction action, RunVariables vars) =>
        VariablesFilterConditions.RowMatchesExtended(this, row, action, vars);

    internal static bool MatchFilter(FlowTable table, List<string> row, string? column, string? op, string? valueTemplate, RunVariables vars, bool ignoreCase)
    {
        if (string.IsNullOrWhiteSpace(column))
            return true;
        var colIdx = table.ResolveColumnIndex(column, null);
        var left = colIdx < row.Count ? row[colIdx] : "";
        var right = RunVariables.Interpolate(valueTemplate ?? "", vars);
        return VariablesSemantics.EvaluateFilter(left, op, right, ignoreCase);
    }

    public static FlowTable Merge(FlowTable a, FlowTable b, string mode)
    {
        mode = VariablesSemantics.NormalizeMergeMode(mode);
        if (ColumnsEqual(a.Columns, b.Columns))
        {
            a.Rows.AddRange(b.Rows.Select(r => new List<string>(r)));
            return a;
        }

        return mode switch
        {
            VariablesEnums.MergeMode.ErrorOnExtraColumns => throw new InvalidOperationException(
                "merge_datatables: column schemas differ (use ignore_extra_columns or add_extra_columns)"),
            VariablesEnums.MergeMode.AddExtraColumns or VariablesEnums.MergeMode.UnionColumns => MergeAddExtraColumns(a, b),
            VariablesEnums.MergeMode.AppendRows or VariablesEnums.MergeMode.IgnoreExtraColumns => MergeIgnoreExtraColumns(a, b),
            _ => MergeIgnoreExtraColumns(a, b),
        };
    }

    static bool ColumnsEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count)
            return false;
        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    static FlowTable MergeIgnoreExtraColumns(FlowTable a, FlowTable b)
    {
        if (a.Columns.Count == 0 && b.Columns.Count > 0)
            a.Columns.AddRange(b.Columns);
        foreach (var row in b.Rows)
            a.Rows.Add(ProjectRow(row, b.Columns, a.Columns));
        return a;
    }

    static FlowTable MergeAddExtraColumns(FlowTable a, FlowTable b)
    {
        var cols = a.Columns.Concat(b.Columns).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var merged = WithColumns(cols);
        foreach (var row in a.Rows)
            merged.Rows.Add(ProjectRow(row, a.Columns, cols));
        foreach (var row in b.Rows)
            merged.Rows.Add(ProjectRow(row, b.Columns, cols));
        return merged;
    }

    static List<string> ProjectRow(List<string> row, List<string> fromCols, List<string> toCols)
    {
        var outRow = new List<string>();
        foreach (var col in toCols)
        {
            var idx = fromCols.FindIndex(c => string.Equals(c, col, StringComparison.OrdinalIgnoreCase));
            outRow.Add(idx >= 0 && idx < row.Count ? row[idx] : "");
        }
        return outRow;
    }

    public static FlowTable Join(
        FlowTable left,
        FlowTable right,
        string joinColumnLeft,
        string? joinColumnRight,
        string operation,
        string? joinComparisonOperator)
    {
        if (left.Columns.Count == 0 || right.Columns.Count == 0)
            throw new InvalidOperationException("join_datatable: both tables need columns");
        var lIdx = left.ResolveColumnIndex(joinColumnLeft, null);
        var rightCol = string.IsNullOrWhiteSpace(joinColumnRight) ? joinColumnLeft : joinColumnRight.Trim();
        var rIdx = right.ResolveColumnIndex(rightCol, null);
        operation = (operation ?? VariablesEnums.JoinOperation.Inner).Trim().ToLowerInvariant();
        var comparison = VariablesSemantics.NormalizeJoinComparison(joinComparisonOperator);
        var useDictionary = VariablesSemantics.JoinComparisonUsesDictionary(comparison);

        var rightOnlyCols = right.Columns
            .Where((c, idx) => idx != rIdx
                && !left.Columns.Any(lc => string.Equals(lc, c, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var outCols = left.Columns.Concat(rightOnlyCols).ToList();
        var result = WithColumns(outCols);

        if (!useDictionary)
            return JoinByComparison(left, right, lIdx, rIdx, operation, comparison, outCols);

        var rightByKey = new Dictionary<string, List<List<string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var rRow in right.Rows)
        {
            var key = rIdx < rRow.Count ? rRow[rIdx] : "";
            if (!rightByKey.TryGetValue(key, out var list))
            {
                list = [];
                rightByKey[key] = list;
            }
            list.Add(rRow);
        }

        var matchedRightKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var lRow in left.Rows)
        {
            var key = lIdx < lRow.Count ? lRow[lIdx] : "";
            if (rightByKey.TryGetValue(key, out var matches))
            {
                matchedRightKeys.Add(key);
                foreach (var rRow in matches)
                    result.Rows.Add(CombineJoinRow(lRow, left.Columns, rRow, right.Columns, outCols));
            }
            else if (operation is VariablesEnums.JoinOperation.Left or VariablesEnums.JoinOperation.Full)
                result.Rows.Add(ProjectRow(lRow, left.Columns, outCols));
        }

        if (operation is VariablesEnums.JoinOperation.Right or VariablesEnums.JoinOperation.Full)
        {
            foreach (var (key, rows) in rightByKey)
            {
                if (matchedRightKeys.Contains(key))
                    continue;
                foreach (var rRow in rows)
                    result.Rows.Add(ProjectRow(rRow, right.Columns, outCols));
            }
        }

        return result;
    }

    static FlowTable JoinByComparison(
        FlowTable left,
        FlowTable right,
        int lIdx,
        int rIdx,
        string operation,
        string comparison,
        List<string> outCols)
    {
        var result = WithColumns(outCols);
        var matchedRight = new HashSet<int>();
        foreach (var lRow in left.Rows)
        {
            var lKey = lIdx < lRow.Count ? lRow[lIdx] : "";
            var any = false;
            for (var ri = 0; ri < right.Rows.Count; ri++)
            {
                var rRow = right.Rows[ri];
                var rKey = rIdx < rRow.Count ? rRow[rIdx] : "";
                if (!VariablesSemantics.EvaluateFilter(lKey, comparison, rKey, ignoreCase: true))
                    continue;
                any = true;
                matchedRight.Add(ri);
                result.Rows.Add(CombineJoinRow(lRow, left.Columns, rRow, right.Columns, outCols));
            }
            if (!any && operation is VariablesEnums.JoinOperation.Left or VariablesEnums.JoinOperation.Full)
                result.Rows.Add(ProjectRow(lRow, left.Columns, outCols));
        }

        if (operation is VariablesEnums.JoinOperation.Right or VariablesEnums.JoinOperation.Full)
        {
            for (var ri = 0; ri < right.Rows.Count; ri++)
            {
                if (matchedRight.Contains(ri))
                    continue;
                result.Rows.Add(ProjectRow(right.Rows[ri], right.Columns, outCols));
            }
        }

        return result;
    }

    static List<string> CombineJoinRow(
        List<string> lRow,
        List<string> lCols,
        List<string> rRow,
        List<string> rCols,
        List<string> outCols)
    {
        var row = ProjectRow(lRow, lCols, outCols);
        foreach (var col in outCols)
        {
            if (lCols.Any(c => string.Equals(c, col, StringComparison.OrdinalIgnoreCase)))
                continue;
            var ri = rCols.FindIndex(c => string.Equals(c, col, StringComparison.OrdinalIgnoreCase));
            var oi = outCols.FindIndex(c => string.Equals(c, col, StringComparison.OrdinalIgnoreCase));
            if (ri >= 0 && oi >= 0)
                row[oi] = ri < rRow.Count ? rRow[ri] : "";
        }
        return row;
    }

    public int FindReplace(VariablesAction action, RunVariables vars)
    {
        var mode = (action.FindReplaceMode ?? VariablesEnums.FindReplaceMode.Replace).Trim().ToLowerInvariant();
        var find = RunVariables.Interpolate(action.FindText ?? "", vars);
        var replace = RunVariables.Interpolate(action.ReplaceText ?? "", vars);
        var area = (action.SearchArea ?? VariablesEnums.FindReplaceArea.Everywhere).Trim().ToLowerInvariant();
        int? colOnly = area == VariablesEnums.FindReplaceArea.Column
            ? ResolveColumnIndex(action.ColumnName, action.ColumnIndex)
            : null;
        var useRegex = action.UseRegex ?? false;
        var matchCase = action.MatchCase ?? false;
        var entireCell = action.MatchEntireCell ?? false;
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var regexOptions = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
        var count = 0;
        for (var r = 0; r < Rows.Count; r++)
        {
            for (var c = 0; c < Columns.Count; c++)
            {
                if (colOnly is { } co && c != co)
                    continue;
                var cell = GetCell(r, c);
                if (string.IsNullOrEmpty(find))
                    continue;
                if (mode == VariablesEnums.FindReplaceMode.Find)
                {
                    if (CellMatches(cell, find, useRegex, entireCell, comparison, regexOptions))
                    {
                        count++;
                        return count;
                    }
                    continue;
                }
                if (!CellMatches(cell, find, useRegex, false, comparison, regexOptions))
                    continue;
                var updated = useRegex
                    ? Regex.Replace(cell, find, replace, regexOptions)
                    : entireCell
                        ? replace
                        : ReplaceFirst(cell, find, replace, comparison);
                if (updated != cell)
                {
                    count++;
                    SetCell(r, c, updated);
                }
            }
        }
        return count;
    }

    static string ReplaceFirst(string cell, string find, string replace, StringComparison comparison)
    {
        var idx = cell.IndexOf(find, comparison);
        return idx < 0 ? cell : cell[..idx] + replace + cell[(idx + find.Length)..];
    }

    static bool CellMatches(string cell, string find, bool useRegex, bool entireCell, StringComparison comparison, RegexOptions regexOptions)
    {
        if (useRegex)
            return Regex.IsMatch(cell, find, regexOptions);
        if (entireCell)
            return string.Equals(cell, find, comparison);
        return cell.Contains(find, comparison);
    }

    public static string ToCsv(FlowTable table, string separator)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(separator, table.Columns.Select(EscapeCsvField)));
        foreach (var row in table.Rows)
        {
            var cells = new List<string>();
            for (var i = 0; i < table.Columns.Count; i++)
                cells.Add(EscapeCsvField(i < row.Count ? row[i] : ""));
            sb.AppendLine(string.Join(separator, cells));
        }
        return sb.ToString();
    }

    static string EscapeCsvField(string field)
    {
        if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }

    public static FlowTable FromFixedWidth(string text, IReadOnlyList<int> widths, bool firstLineIsHeader)
    {
        if (widths.Count == 0)
            throw new InvalidOperationException("column_widths required for fixed_width CSV");
        var table = new FlowTable();
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var cells = SplitFixedWidthLine(line, widths);
            if (i == 0 && firstLineIsHeader)
            {
                table.Columns.AddRange(cells);
                continue;
            }
            if (table.Columns.Count == 0)
            {
                for (var c = 0; c < cells.Count; c++)
                    table.Columns.Add($"Column{c + 1}");
            }
            table.NormalizeRowWidth(cells);
            table.Rows.Add(cells);
        }
        return table;
    }

    static List<string> SplitFixedWidthLine(string line, IReadOnlyList<int> widths)
    {
        var cells = new List<string>();
        var pos = 0;
        foreach (var width in widths)
        {
            if (pos >= line.Length)
            {
                cells.Add("");
                continue;
            }
            var len = Math.Min(width, line.Length - pos);
            cells.Add(line.Substring(pos, len).Trim());
            pos += width;
        }
        return cells;
    }

    public static List<int> ParseColumnWidths(string? template, RunVariables vars)
    {
        var raw = RunVariables.Interpolate(template ?? "", vars).Trim();
        if (string.IsNullOrEmpty(raw))
            return [];
        if (raw.StartsWith('['))
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.EnumerateArray()
                .Select(e => int.Parse(e.ToString(), CultureInfo.InvariantCulture))
                .ToList();
        }
        return raw.Split(',', StringSplitOptions.TrimEntries)
            .Select(s => int.Parse(s, CultureInfo.InvariantCulture))
            .ToList();
    }

    public static FlowTable FromCsv(string text, char separator, bool firstLineIsHeader)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var table = new FlowTable();
        for (var i = 0; i < lines.Length; i++)
        {
            var cells = ParseCsvLine(lines[i], separator);
            if (i == 0 && firstLineIsHeader)
            {
                table.Columns.AddRange(cells);
                continue;
            }
            if (table.Columns.Count == 0)
            {
                for (var c = 0; c < cells.Count; c++)
                    table.Columns.Add($"Column{c + 1}");
            }
            table.NormalizeRowWidth(cells);
            table.Rows.Add(cells);
        }
        return table;
    }

    static List<string> ParseCsvLine(string line, char separator)
    {
        var cells = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                        inQuotes = false;
                }
                else
                    sb.Append(ch);
            }
            else if (ch == '"')
                inQuotes = true;
            else if (ch == separator)
            {
                cells.Add(sb.ToString());
                sb.Clear();
            }
            else
                sb.Append(ch);
        }
        cells.Add(sb.ToString());
        return cells;
    }

    public static List<string> ParseRowValues(string? valuesTemplate, RunVariables vars)
    {
        var raw = RunVariables.Interpolate(valuesTemplate ?? "", vars).Trim();
        if (string.IsNullOrEmpty(raw))
            return [];
        if (raw.StartsWith('['))
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.EnumerateArray().Select(CellToString).ToList();
        }
        return raw.Split(',', StringSplitOptions.TrimEntries).ToList();
    }
}
