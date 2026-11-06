using Hoblio.Model;

namespace Hoblio.Studio;

static class PdfPaletteCatalog
{
    public static PaletteGroup Group => new()
    {
        Name = "PDF",
        Items =
        [
            new($"action:{PdfActionTypes.ExtractText}", "Extrair texto do PDF", "ExtractTextFromPDF Pdf text",
                "Lê ficheiro .pdf para variável (UglyToad.PdfPig)"),
            new($"action:{PdfActionTypes.ExtractTables}", "Extrair tabelas (heurística)", "ExtractTablesFromPDF",
                "Pdf tabelas JSON PdfPig"),
            new($"action:{PdfActionTypes.ExtractImages}", "Extrair imagens do PDF", "ExtractImagesFromPDF",
                "XObject imagens png"),
            new($"action:{PdfActionTypes.ExtractPages}", "Extrair páginas para novo PDF", "ExtractPagesToNewPDF",
                "split páginas"),
            new($"action:{PdfActionTypes.Merge}", "Unir PDFs", "MergePDFFiles",
                "merge PdfMerger"),
        ],
    };
}
