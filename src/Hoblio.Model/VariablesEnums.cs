namespace Hoblio.Model;

public static class VariablesEnums
{
    public static class AddRowMode
    {
        public const string Append = "append";
        public const string Insert = "insert";
    }

    public static class AddColumnMode
    {
        public const string Append = "append";
        public const string Insert = "insert";
    }

    public static class ColumnSpecifier
    {
        public const string ByName = "name";
        public const string ByIndex = "index";
    }

    public static class SortDirection
    {
        public const string Asc = "asc";
        public const string Desc = "desc";
    }

    public static class MergeMode
    {
        /// <summary>PAD: <c>MergeMode.AddExtraColumns</c> — união de colunas e linhas.</summary>
        public const string AddExtraColumns = "add_extra_columns";

        /// <summary>PAD: <c>MergeMode.ErrorOnExtraColumns</c>.</summary>
        public const string ErrorOnExtraColumns = "error_on_extra_columns";

        /// <summary>PAD: <c>MergeMode.IgnoreExtraColumns</c> — projeta a 2ª tabela no esquema da 1ª.</summary>
        public const string IgnoreExtraColumns = "ignore_extra_columns";

        /// <summary>Legado: equivale a <see cref="IgnoreExtraColumns"/> se colunas diferirem.</summary>
        public const string AppendRows = "append_rows";

        /// <summary>Legado: alias de <see cref="AddExtraColumns"/>.</summary>
        public const string UnionColumns = "union_columns";
    }

    public static class JoinOperation
    {
        public const string Inner = "inner";
        public const string Left = "left";
        public const string Right = "right";
        public const string Full = "full";
    }

    public static class JoinComparisonOperator
    {
        public const string Eq = "eq";
        public const string Ne = "ne";
    }

    public static class FilterOperator
    {
        public const string Eq = "eq";
        public const string Ne = "ne";
        public const string Contains = "contains";
        public const string NotContains = "not_contains";
        public const string BeginsWith = "begins_with";
        public const string EndsWith = "ends_with";
        public const string IsEmpty = "is_empty";
        public const string IsNotEmpty = "is_not_empty";
        public const string Gt = "gt";
        public const string Gte = "gte";
        public const string Lt = "lt";
        public const string Lte = "lte";
    }

    public static class FilterLogic
    {
        public const string And = "and";
        public const string Or = "or";
    }

    public static class FindReplaceMode
    {
        public const string Find = "find";
        public const string Replace = "replace";
    }

    public static class FindReplaceArea
    {
        public const string Everywhere = "everywhere";
        public const string Column = "column";
    }

    public static class RemoveListItemMode
    {
        public const string ByIndex = "by_index";
        public const string ByValue = "by_value";
    }

    public static class TruncateOperation
    {
        public const string Round = "round";
        public const string IntegerPart = "integer";
        public const string DecimalPart = "decimal";
        public const string Truncate = "truncate";
    }

    public static class CsvSeparator
    {
        public const string Comma = "comma";
        public const string Semicolon = "semicolon";
        public const string Tab = "tab";
        public const string Custom = "custom";
    }

    public static class CsvReadMode
    {
        public const string Delimiter = "delimiter";
        public const string FixedWidth = "fixed_width";
    }

    public static class RandomNumberMode
    {
        public const string Single = "single";
        public const string List = "list";
        public const string Integer = "integer";
        public const string IntegerList = "integer_list";
    }
}
