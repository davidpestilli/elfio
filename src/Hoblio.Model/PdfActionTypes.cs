namespace Hoblio.Model;

/// <summary>JSON <c>type</c> and telemetry <c>action_id</c> for PDF actions (PAD group PDF).</summary>
public static class PdfActionTypes
{
    public const string ExtractText = "pdf_extract_text";
    public const string ExtractTables = "pdf_extract_tables";
    public const string ExtractImages = "pdf_extract_images";
    public const string ExtractPages = "pdf_extract_pages";
    public const string Merge = "pdf_merge";

    public static readonly IReadOnlyList<string> All =
    [
        ExtractText,
        ExtractTables,
        ExtractImages,
        ExtractPages,
        Merge,
    ];

    public static readonly IReadOnlyDictionary<string, string> ModuleActionIds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ExtractText] = "ExtractTextFromPDF",
            [ExtractTables] = "ExtractTablesFromPDF",
            [ExtractImages] = "ExtractImagesFromPDF",
            [ExtractPages] = "ExtractPagesToNewPDF",
            [Merge] = "MergePDFFiles",
        };

    public static bool IsPdfType(string? type) =>
        type is not null && ModuleActionIds.ContainsKey(type);

    public static string ModuleActionId(string type) =>
        ModuleActionIds.TryGetValue(type, out var id) ? id : type;
}
