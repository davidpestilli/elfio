namespace Hoblio.Model;

/// <summary>Listas, tabelas de dados, números e JSON (paleta Variáveis PAD).</summary>
public sealed class VariablesAction : ActionKind
{
    public string Operation { get; init; } = VariablesActionTypes.CreateDatatable;

    public override string Type => Operation;

    /// <summary>Variável que contém ou recebe a tabela de dados (JSON).</summary>
    public string? DataTableVariable { get; init; }

    /// <summary>Segunda tabela (merge/join).</summary>
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

    /// <summary>Larguras fixas CSV (ex. <c>10,20,15</c> ou JSON array).</summary>
    public string? ColumnWidths { get; init; }

    public string? FilterColumn { get; init; }
    public string? FilterOperator { get; init; }
    public string? FilterValue { get; init; }
    public string? FilterColumn2 { get; init; }
    public string? FilterOperator2 { get; init; }
    public string? FilterValue2 { get; init; }
    public string? FilterLogic { get; init; }

    /// <summary>Condições extras (3+) como JSON array: <c>[{ "column", "operator", "value", "logic"? }, …]</c>.</summary>
    public string? FilterConditions { get; init; }

    public string? JoinColumn { get; init; }
    /// <summary>Coluna equivalente na 2ª tabela quando o nome difere (<c>join_column2</c> PAD).</summary>
    public string? JoinColumn2 { get; init; }
    public string? JoinOperation { get; init; }
    /// <summary>Operador de comparação entre chaves (<c>join_comparison_operator</c> PAD; padrão <c>eq</c>).</summary>
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

    public override IEnumerable<string> ProducedVariables()
    {
        if (!string.IsNullOrWhiteSpace(OutputVariable))
            yield return OutputVariable.Trim();
        if (!string.IsNullOrWhiteSpace(OutputVariable2))
            yield return OutputVariable2.Trim();

        if (Type is VariablesActionTypes.CreateDatatable or VariablesActionTypes.ReadCsv
            && !string.IsNullOrWhiteSpace(DataTableVariable))
            yield return DataTableVariable.Trim();

        if (Type == VariablesActionTypes.CreateList && !string.IsNullOrWhiteSpace(ListVariable))
            yield return ListVariable.Trim();

        if (Type == VariablesActionTypes.ColumnToList && !string.IsNullOrWhiteSpace(ListVariable))
            yield return ListVariable.Trim();
    }
}
