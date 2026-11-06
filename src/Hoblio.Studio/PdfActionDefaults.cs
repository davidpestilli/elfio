using Hoblio.Model;

namespace Hoblio.Studio;

static class PdfActionDefaults
{
    public static PdfAction? TryCreate(string type, IReadOnlySet<string>? takenNames)
    {
        if (!PdfActionTypes.IsPdfType(type))
            return null;

        var output = NextName("PdfText", takenNames) ?? "PdfText";
        return type switch
        {
            PdfActionTypes.ExtractText => new PdfAction
            {
                Operation = type,
                Path = "documento.pdf",
                Mode = PdfEnums.ModeAll,
                OutputVariable = output,
            },
            PdfActionTypes.ExtractTables => new PdfAction
            {
                Operation = type,
                Path = "documento.pdf",
                Mode = PdfEnums.ModeAll,
                RowTolerance = "4",
                OutputVariable = NextName("PdfTables", takenNames) ?? "PdfTables",
            },
            PdfActionTypes.ExtractImages => new PdfAction
            {
                Operation = type,
                Path = "documento.pdf",
                Mode = PdfEnums.ModeAll,
                OutputPath = "output/pdf_images",
                ImageFormat = "png",
                OutputVariable = NextName("PdfImageFiles", takenNames) ?? "PdfImageFiles",
            },
            PdfActionTypes.ExtractPages => new PdfAction
            {
                Operation = type,
                Path = "origem.pdf",
                Mode = PdfEnums.ModeRange,
                StartPage = "1",
                EndPage = "1",
                OutputPath = "output/extracto.pdf",
                OutputVariable = NextName("PdfExtractPath", takenNames) ?? "PdfExtractPath",
            },
            PdfActionTypes.Merge => new PdfAction
            {
                Operation = type,
                Path = "output/unido.pdf",
                OutputPath = "output/unido.pdf",
                InputPaths = "a.pdf\nb.pdf",
            },
            _ => null,
        };
    }

    static string? NextName(string prefix, IReadOnlySet<string>? takenNames)
    {
        if (takenNames is null || takenNames.Count == 0)
            return null;
        for (var i = 1; i <= 999; i++)
        {
            var candidate = i == 1 ? prefix : prefix + i;
            if (!takenNames.Contains(candidate))
                return candidate;
        }
        return null;
    }
}
