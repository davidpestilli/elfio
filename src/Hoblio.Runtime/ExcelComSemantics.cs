using Hoblio.Model;

namespace Hoblio.Runtime;

static class ExcelComSemantics
{
    public const int XlAnd = 1;
    public const int XlOr = 2;

    public static string BuildFilterCriteria(string? op, string rawValue, RunVariables vars)
    {
        var value = RunVariables.Interpolate(rawValue, vars);
        var oper = (op ?? ExcelEnums.FilterOperator.Equal).Trim().ToLowerInvariant();
        return oper switch
        {
            ExcelEnums.FilterOperator.NotEquals => $"<> {QuoteIfNeeded(value)}",
            ExcelEnums.FilterOperator.GreaterThan => $"> {QuoteIfNeeded(value)}",
            ExcelEnums.FilterOperator.GreaterOrEqual => $">= {QuoteIfNeeded(value)}",
            ExcelEnums.FilterOperator.LessThan => $"< {QuoteIfNeeded(value)}",
            ExcelEnums.FilterOperator.LessOrEqual => $"<= {QuoteIfNeeded(value)}",
            ExcelEnums.FilterOperator.Contains => value.Contains('*') ? value : $"=*{value}*",
            ExcelEnums.FilterOperator.BeginsWith => value.EndsWith('*') ? value : $"{value}*",
            ExcelEnums.FilterOperator.EndsWith => value.StartsWith('*') ? value : $"*{value}",
            _ => value.Contains('=') || value.Contains('*') || value.Contains('<') || value.Contains('>')
                ? value
                : $"={QuoteIfNeeded(value)}",
        };
    }

    static string QuoteIfNeeded(string value) =>
        double.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out _)
            ? value
            : value;

    public static int FilterLogicOperator(string? logic) =>
        string.Equals(logic, ExcelEnums.FilterLogic.Or, StringComparison.OrdinalIgnoreCase) ? XlOr : XlAnd;

    public static int ParsePasteType(string? mode)
    {
        return (mode ?? ExcelEnums.PasteMode.All).Trim().ToLowerInvariant() switch
        {
            ExcelEnums.PasteMode.Values => -4163,
            ExcelEnums.PasteMode.Formulas => -4123,
            ExcelEnums.PasteMode.Formats => -4122,
            ExcelEnums.PasteMode.ValuesAndFormats => -4163,
            _ => -4104,
        };
    }

    public static bool SortDescending(string? order) =>
        string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
}
