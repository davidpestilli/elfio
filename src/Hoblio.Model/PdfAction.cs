namespace Hoblio.Model;

/// <summary>Read PDF files and extract text (no Acrobat COM; uses PdfPig at runtime).</summary>
public sealed class PdfAction : ActionKind
{
    public string Operation { get; init; } = PdfActionTypes.ExtractText;

    public override string Type => Operation;

    /// <summary>Path to the PDF (<c>{var}</c> supported).</summary>
    public required string Path { get; init; }

    /// <summary><see cref="PdfEnums.ModeAll"/>, <see cref="PdfEnums.ModePage"/>, or <see cref="PdfEnums.ModeRange"/>.</summary>
    public string? Mode { get; init; }

    /// <summary>1-based page index when <c>mode=page</c>.</summary>
    public string? Page { get; init; }

    /// <summary>Range start (1-based, inclusive) when <c>mode=range</c>.</summary>
    public string? StartPage { get; init; }

    /// <summary>Range end (1-based, inclusive) when <c>mode=range</c>.</summary>
    public string? EndPage { get; init; }

    /// <summary>Optional owner/user password for encrypted PDFs.</summary>
    public string? Password { get; init; }

    /// <summary>
    /// Destination path for <see cref="PdfActionTypes.ExtractPages"/> or <see cref="PdfActionTypes.Merge"/> (new PDF);
    /// folder for <see cref="PdfActionTypes.ExtractImages"/> (images saved as <c>page_{n}_{i}.ext</c>).
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// One PDF path per line for <see cref="PdfActionTypes.Merge"/> (<c>{var}</c> per line).
    /// </summary>
    public string? InputPaths { get; init; }

    /// <summary>png or jpg when saving images from <see cref="PdfActionTypes.ExtractImages"/>.</summary>
    public string? ImageFormat { get; init; }

    /// <summary>Heuristic: maximum vertical gap (points) to group words on the same row (tables).</summary>
    public string? RowTolerance { get; init; }

    /// <summary>Optional: JSON/text summary or extracted content (always set for extract_text/tables; optional for merge/pages/images).</summary>
    public string? OutputVariable { get; init; }

    public override IEnumerable<string> ProducedVariables()
    {
        if (!string.IsNullOrWhiteSpace(OutputVariable))
            yield return OutputVariable.Trim();
    }
}
