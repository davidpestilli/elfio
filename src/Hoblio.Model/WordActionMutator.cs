namespace Hoblio.Model;

public static class WordActionMutator
{
    public static WordAction RenameReferences(WordAction a, Func<string, string> replaceTemplate, Func<string?, string?> renameVar) =>
        new()
        {
            Operation = a.Operation,
            Instance = renameVar(a.Instance) ?? a.Instance,
            Path = ReplaceOpt(a.Path, replaceTemplate),
            Visible = a.Visible,
            ReadOnly = a.ReadOnly,
            DocumentName = ReplaceOpt(a.DocumentName, replaceTemplate),
            Mode = ReplaceOpt(a.Mode, replaceTemplate),
            Pages = ReplaceOpt(a.Pages, replaceTemplate),
            Bookmark = ReplaceOpt(a.Bookmark, replaceTemplate),
            Value = ReplaceOpt(a.Value, replaceTemplate),
            AppendNewLine = a.AppendNewLine,
            OutputVariable = renameVar(a.OutputVariable),
            FindText = ReplaceOpt(a.FindText, replaceTemplate),
            ReplaceText = ReplaceOpt(a.ReplaceText, replaceTemplate),
            AllMatches = a.AllMatches,
            MatchWildcards = a.MatchWildcards,
            MatchCase = a.MatchCase,
            MatchEntireWord = a.MatchEntireWord,
            ImageSource = ReplaceOpt(a.ImageSource, replaceTemplate),
            TextToFind = ReplaceOpt(a.TextToFind, replaceTemplate),
        };

    static string? ReplaceOpt(string? value, Func<string, string> replace) =>
        value is null ? null : replace(value);

    public static WordAction With(WordAction a, WordActionPatch patch) =>
        new()
        {
            Operation = a.Operation,
            Instance = patch.Instance ?? a.Instance,
            Path = patch.Path ?? a.Path,
            Visible = patch.Visible ?? a.Visible,
            ReadOnly = patch.ReadOnly ?? a.ReadOnly,
            DocumentName = patch.DocumentName ?? a.DocumentName,
            Mode = patch.Mode ?? a.Mode,
            Pages = patch.Pages ?? a.Pages,
            Bookmark = patch.Bookmark ?? a.Bookmark,
            Value = patch.Value ?? a.Value,
            AppendNewLine = patch.AppendNewLine ?? a.AppendNewLine,
            OutputVariable = patch.OutputVariable ?? a.OutputVariable,
            FindText = patch.FindText ?? a.FindText,
            ReplaceText = patch.ReplaceText ?? a.ReplaceText,
            AllMatches = patch.AllMatches ?? a.AllMatches,
            MatchWildcards = patch.MatchWildcards ?? a.MatchWildcards,
            MatchCase = patch.MatchCase ?? a.MatchCase,
            MatchEntireWord = patch.MatchEntireWord ?? a.MatchEntireWord,
            ImageSource = patch.ImageSource ?? a.ImageSource,
            TextToFind = patch.TextToFind ?? a.TextToFind,
        };
}

public sealed class WordActionPatch
{
    public string? Instance { get; init; }
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
}
