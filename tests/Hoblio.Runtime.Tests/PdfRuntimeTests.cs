using Hoblio.Model;
using Hoblio.Runtime;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Hoblio.Runtime.Tests;

public sealed class PdfRuntimeTests
{
    [Fact]
    public void Pdf_extract_text_all_pages()
    {
        var path = Path.Combine(Path.GetTempPath(), $"elfio-test-{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(path, BuildTwoPagePdf("Hello Alpha", "Beta Gamma"));

        var vars = new RunVariables();
        using var pdf = new PdfContext();
        var action = new PdfAction
        {
            Operation = PdfActionTypes.ExtractText,
            Path = path,
            Mode = PdfEnums.ModeAll,
            OutputVariable = "Out",
        };

        var summary = pdf.Execute(action, vars, null);
        Assert.Contains("pdf_extract_text", summary);
        Assert.Contains("Hello", vars["Out"], StringComparison.Ordinal);
        Assert.Contains("Beta", vars["Out"], StringComparison.Ordinal);
        File.Delete(path);
    }

    [Fact]
    public void Pdf_extract_text_single_page()
    {
        var path = Path.Combine(Path.GetTempPath(), $"elfio-test-{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(path, BuildTwoPagePdf("Page1", "Page2Only"));

        var vars = new RunVariables();
        using var pdf = new PdfContext();
        var action = new PdfAction
        {
            Operation = PdfActionTypes.ExtractText,
            Path = path,
            Mode = PdfEnums.ModePage,
            Page = "2",
            OutputVariable = "T",
        };

        pdf.Execute(action, vars, null);
        Assert.Contains("Page2Only", vars["T"], StringComparison.Ordinal);
        Assert.DoesNotContain("Page1", vars["T"], StringComparison.Ordinal);
        File.Delete(path);
    }

    [Fact]
    public void Pdf_merge_and_extract_page_two()
    {
        var dir = Path.Combine(Path.GetTempPath(), "elfio-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var aPath = Path.Combine(dir, "a.pdf");
        var bPath = Path.Combine(dir, "b.pdf");
        var mergedPath = Path.Combine(dir, "merged.pdf");
        var page2Path = Path.Combine(dir, "page2.pdf");

        File.WriteAllBytes(aPath, BuildTwoPagePdf("OnlyA1", "OnlyA2"));
        File.WriteAllBytes(bPath, BuildTwoPagePdf("B1Line", "B2Line"));

        var vars = new RunVariables();
        using var pdf = new PdfContext();

        pdf.Execute(new PdfAction
        {
            Operation = PdfActionTypes.Merge,
            Path = mergedPath,
            OutputPath = mergedPath,
            InputPaths = aPath + "\n" + bPath,
        }, vars, null);
        Assert.True(File.Exists(mergedPath));

        var extractPagesSummary = pdf.Execute(new PdfAction
        {
            Operation = PdfActionTypes.ExtractPages,
            Path = mergedPath,
            Mode = PdfEnums.ModePage,
            Page = "2",
            OutputPath = page2Path,
        }, vars, null);

        Assert.True(File.Exists(page2Path));
        Assert.Contains("pdf_extract_pages", extractPagesSummary);

        pdf.Execute(new PdfAction
        {
            Operation = PdfActionTypes.ExtractText,
            Path = page2Path,
            Mode = PdfEnums.ModeAll,
            OutputVariable = "T2",
        }, vars, null);

        Assert.Contains("OnlyA2", vars["T2"], StringComparison.Ordinal);

        try
        {
            Directory.Delete(dir, recursive: true);
        }
        catch
        {
            /* ignore cleanup on locked temp dirs */
        }
    }

    static byte[] BuildTwoPagePdf(string line1, string line2)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var p1 = builder.AddPage(PageSize.A4);
        var top = new PdfPoint(40, p1.PageSize.Top - 40);
        p1.AddText(line1, 12, top, font);
        var p2 = builder.AddPage(PageSize.A4);
        p2.AddText(line2, 12, new PdfPoint(40, p2.PageSize.Top - 40), font);
        return builder.Build();
    }
}
