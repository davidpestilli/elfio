namespace Hoblio.Model;

/// <summary>Automação Microsoft Excel (sessão COM compartilhada via variável <see cref="Instance"/>).</summary>
public sealed class ExcelAction : ActionKind
{
    public string Operation { get; init; } = ExcelActionTypes.Launch;

    public override string Type => Operation;

    /// <summary>Nome da variável de fluxo que guarda o identificador da sessão Excel.</summary>
    public string Instance { get; init; } = "ExcelInstance";

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

    /// <summary>Segunda instância Excel (ex.: colar/copiar planilha em outro workbook).</summary>
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

    public override IEnumerable<string> ProducedVariables()
    {
        if (Type == ExcelActionTypes.Launch && !string.IsNullOrWhiteSpace(Instance))
            yield return Instance.Trim();

        if (!string.IsNullOrWhiteSpace(OutputVariable))
            yield return OutputVariable.Trim();
        if (!string.IsNullOrWhiteSpace(OutputVariable2))
            yield return OutputVariable2.Trim();
    }
}
