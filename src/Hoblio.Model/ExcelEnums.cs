namespace Hoblio.Model;

/// <summary>Valores aceitos em campos string de <see cref="ExcelAction"/> (modos / operadores).</summary>
public static class ExcelEnums
{
    public static class PasteMode
    {
        public const string All = "all";
        public const string Values = "values";
        public const string Formulas = "formulas";
        public const string Formats = "formats";
        public const string ValuesAndFormats = "values_formats";
    }

    public static class FilterOperator
    {
        public const string Equal = "eq";
        public const string NotEquals = "ne";
        public const string GreaterThan = "gt";
        public const string GreaterOrEqual = "gte";
        public const string LessThan = "lt";
        public const string LessOrEqual = "lte";
        public const string Contains = "contains";
        public const string BeginsWith = "begins";
        public const string EndsWith = "ends";
    }

    public static class FilterLogic
    {
        public const string And = "and";
        public const string Or = "or";
    }

    public static class DeleteScope
    {
        public const string Cells = "cells";
        public const string Rows = "rows";
        public const string Columns = "columns";
    }

    public static class ClearFilterScope
    {
        public const string All = "all";
        public const string Column = "column";
        public const string Table = "table";
    }

    public static class WorksheetPosition
    {
        public const string After = "after";
        public const string Before = "before";
        public const string Begin = "begin";
        public const string End = "end";
    }
}
