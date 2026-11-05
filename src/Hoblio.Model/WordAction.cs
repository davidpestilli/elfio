namespace Hoblio.Model;

/// <summary>Automação Microsoft Word (sessão COM via variável <see cref="Instance"/>).</summary>
public sealed class WordAction : ActionKind
{
    public string Operation { get; init; } = WordActionTypes.Launch;

    public override string Type => Operation;

    public string Instance { get; init; } = "WordInstance";

    public string? Path { get; init; }
    public bool? Visible { get; init; }
    public bool? ReadOnly { get; init; }
    public string? DocumentName { get; init; }
    public string? Mode { get; init; }
    public string? Pages { get; init; }
    public string? Bookmark { get; init; }
    public string? Value { get; init; }
    public bool? AppendNewLine { get; init; }
    public string? OutputVariable { get; init; }
    public string? FindText { get; init; }
    public string? ReplaceText { get; init; }
    public bool? AllMatches { get; init; }
    public bool? MatchWildcards { get; init; }
    public bool? MatchCase { get; init; }
    public bool? MatchEntireWord { get; init; }
    public string? ImageSource { get; init; }
    public string? TextToFind { get; init; }

    public override IEnumerable<string> ProducedVariables()
    {
        if (Type == WordActionTypes.Launch && !string.IsNullOrWhiteSpace(Instance))
            yield return Instance.Trim();

        if (!string.IsNullOrWhiteSpace(OutputVariable))
            yield return OutputVariable.Trim();
    }
}
