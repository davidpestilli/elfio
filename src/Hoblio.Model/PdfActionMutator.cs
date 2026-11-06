namespace Hoblio.Model;

public static class PdfActionMutator
{
    public static PdfAction RenameReferences(PdfAction a, Func<string, string> replaceTemplate, Func<string?, string?> renameVar) =>
        new()
        {
            Operation = a.Operation,
            Path = replaceTemplate(a.Path),
            Mode = ReplaceOpt(a.Mode, replaceTemplate),
            Page = ReplaceOpt(a.Page, replaceTemplate),
            StartPage = ReplaceOpt(a.StartPage, replaceTemplate),
            EndPage = ReplaceOpt(a.EndPage, replaceTemplate),
            Password = ReplaceOpt(a.Password, replaceTemplate),
            OutputPath = ReplaceOpt(a.OutputPath, replaceTemplate),
            InputPaths = ReplaceOpt(a.InputPaths, replaceTemplate),
            ImageFormat = ReplaceOpt(a.ImageFormat, replaceTemplate),
            RowTolerance = ReplaceOpt(a.RowTolerance, replaceTemplate),
            OutputVariable = renameVar(a.OutputVariable) ?? a.OutputVariable,
        };

    static string? ReplaceOpt(string? value, Func<string, string> replace) =>
        value is null ? null : replace(value);

    public static PdfAction With(PdfAction a, PdfActionPatch patch) =>
        new()
        {
            Operation = a.Operation,
            Path = patch.Path ?? a.Path,
            Mode = patch.Mode ?? a.Mode,
            Page = patch.Page ?? a.Page,
            StartPage = patch.StartPage ?? a.StartPage,
            EndPage = patch.EndPage ?? a.EndPage,
            Password = patch.Password ?? a.Password,
            OutputPath = patch.OutputPath ?? a.OutputPath,
            InputPaths = patch.InputPaths ?? a.InputPaths,
            ImageFormat = patch.ImageFormat ?? a.ImageFormat,
            RowTolerance = patch.RowTolerance ?? a.RowTolerance,
            OutputVariable = patch.OutputVariable ?? a.OutputVariable,
        };
}

public sealed class PdfActionPatch
{
    public string? Path { get; init; }
    public string? Mode { get; init; }
    public string? Page { get; init; }
    public string? StartPage { get; init; }
    public string? EndPage { get; init; }
    public string? Password { get; init; }
    public string? OutputPath { get; init; }
    public string? InputPaths { get; init; }
    public string? ImageFormat { get; init; }
    public string? RowTolerance { get; init; }
    public string? OutputVariable { get; init; }
}
