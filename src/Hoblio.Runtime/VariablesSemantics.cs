using Hoblio.Model;

namespace Hoblio.Runtime;

/// <summary>Operadores e modos alinhados ao PAD Variáveis e vocabulário Excel (<see cref="ExcelEnums"/>).</summary>
public static class VariablesSemantics
{
    public static string NormalizeJoinComparison(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return VariablesEnums.JoinComparisonOperator.Eq;
        var op = NormalizeFilterOperator(raw);
        return op switch
        {
            VariablesEnums.FilterOperator.Eq or VariablesEnums.FilterOperator.Ne
                or VariablesEnums.FilterOperator.Gt or VariablesEnums.FilterOperator.Gte
                or VariablesEnums.FilterOperator.Lt or VariablesEnums.FilterOperator.Lte
                => op,
            _ => VariablesEnums.JoinComparisonOperator.Eq,
        };
    }

    public static bool JoinComparisonUsesDictionary(string comparison) =>
        comparison == VariablesEnums.JoinComparisonOperator.Eq;

    public static string NormalizeFilterOperator(string? raw)
    {
        var op = (raw ?? VariablesEnums.FilterOperator.Eq).Trim().ToLowerInvariant();
        return op switch
        {
            "equal" or "equals" or ExcelEnums.FilterOperator.Equal => VariablesEnums.FilterOperator.Eq,
            "notequal" or "not_equals" or "not_equal" or ExcelEnums.FilterOperator.NotEquals => VariablesEnums.FilterOperator.Ne,
            "greaterthan" or ExcelEnums.FilterOperator.GreaterThan => VariablesEnums.FilterOperator.Gt,
            "greaterorequal" or ExcelEnums.FilterOperator.GreaterOrEqual => VariablesEnums.FilterOperator.Gte,
            "lessthan" or ExcelEnums.FilterOperator.LessThan => VariablesEnums.FilterOperator.Lt,
            "lessorequal" or ExcelEnums.FilterOperator.LessOrEqual => VariablesEnums.FilterOperator.Lte,
            "begins" or "beginswith" or "starts_with" or "startswith" => VariablesEnums.FilterOperator.BeginsWith,
            "ends" or "endswith" or "ends_with" => VariablesEnums.FilterOperator.EndsWith,
            "notcontains" or "doesnotcontain" or "not_contains" => VariablesEnums.FilterOperator.NotContains,
            "isempty" or "is_empty" or "blank" or "isblank" => VariablesEnums.FilterOperator.IsEmpty,
            "isnotempty" or "is_not_empty" or "notblank" => VariablesEnums.FilterOperator.IsNotEmpty,
            _ => op,
        };
    }

    public static bool EvaluateFilter(string left, string? operatorRaw, string right, bool ignoreCase)
    {
        var op = NormalizeFilterOperator(operatorRaw);
        if (ignoreCase)
        {
            left = left.ToLowerInvariant();
            right = right.ToLowerInvariant();
        }

        return op switch
        {
            VariablesEnums.FilterOperator.Eq => left == right,
            VariablesEnums.FilterOperator.Ne => left != right,
            VariablesEnums.FilterOperator.Contains => left.Contains(right, StringComparison.Ordinal),
            VariablesEnums.FilterOperator.NotContains => !left.Contains(right, StringComparison.Ordinal),
            VariablesEnums.FilterOperator.BeginsWith => left.StartsWith(right, StringComparison.Ordinal),
            VariablesEnums.FilterOperator.EndsWith => left.EndsWith(right, StringComparison.Ordinal),
            VariablesEnums.FilterOperator.IsEmpty => string.IsNullOrWhiteSpace(left),
            VariablesEnums.FilterOperator.IsNotEmpty => !string.IsNullOrWhiteSpace(left),
            VariablesEnums.FilterOperator.Gt => CompareNumeric(left, right) > 0,
            VariablesEnums.FilterOperator.Gte => CompareNumeric(left, right) >= 0,
            VariablesEnums.FilterOperator.Lt => CompareNumeric(left, right) < 0,
            VariablesEnums.FilterOperator.Lte => CompareNumeric(left, right) <= 0,
            _ => left == right,
        };
    }

    static int CompareNumeric(string left, string right)
    {
        var l = ExpressionEvaluator.ParseNumber(left);
        var r = ExpressionEvaluator.ParseNumber(right);
        return l.CompareTo(r);
    }

    public static string NormalizeMergeMode(string? raw)
    {
        var mode = (raw ?? VariablesEnums.MergeMode.IgnoreExtraColumns).Trim().ToLowerInvariant();
        return mode switch
        {
            "add_extra_columns" or "addextracolumns" => VariablesEnums.MergeMode.AddExtraColumns,
            "error_on_extra_columns" or "erroronextracolumns" => VariablesEnums.MergeMode.ErrorOnExtraColumns,
            "ignore_extra_columns" or "ignoreextracolumns" => VariablesEnums.MergeMode.IgnoreExtraColumns,
            // legado Elfio v1
            "append_rows" or "append" => VariablesEnums.MergeMode.AppendRows,
            "union_columns" or "union" => VariablesEnums.MergeMode.AddExtraColumns,
            _ => mode,
        };
    }

    public static string NormalizeTruncateOperation(string? raw)
    {
        var op = (raw ?? VariablesEnums.TruncateOperation.Round).Trim().ToLowerInvariant();
        return op switch
        {
            "getintegerpart" or "get_integer_part" or "integerpart" => VariablesEnums.TruncateOperation.IntegerPart,
            "getdecimalpart" or "get_decimal_part" or "decimalpart" => VariablesEnums.TruncateOperation.DecimalPart,
            "roundatnthdecimaldigits" or "round_at_nth" or "round_number" or "roundnumber" => VariablesEnums.TruncateOperation.Round,
            "truncate" or "truncate_at" => VariablesEnums.TruncateOperation.Truncate,
            _ => op,
        };
    }

    public static string NormalizeCsvReadMode(string? raw)
    {
        var mode = (raw ?? VariablesEnums.CsvReadMode.Delimiter).Trim().ToLowerInvariant();
        return mode switch
        {
            "fixed_width" or "fixedwidth" or "fixed_column_widths" or "fixed" => VariablesEnums.CsvReadMode.FixedWidth,
            "delimiter" or "separator" or "custom_separator" => VariablesEnums.CsvReadMode.Delimiter,
            _ => mode,
        };
    }

    public static bool SortDescending(string? order) =>
        string.Equals(order, VariablesEnums.SortDirection.Desc, StringComparison.OrdinalIgnoreCase)
        || string.Equals(order, "descending", StringComparison.OrdinalIgnoreCase);
}
