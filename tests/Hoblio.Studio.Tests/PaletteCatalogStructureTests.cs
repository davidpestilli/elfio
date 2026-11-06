using Hoblio.Studio;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class PaletteCatalogStructureTests
{
    [Fact]
    public void Excel_is_single_group_with_basic_and_advanced_sections()
    {
        var excelGroups = ActionPaletteCatalog.Groups.Where(g => g.Name == "Excel").ToList();
        Assert.Single(excelGroups);
        var excel = excelGroups[0];
        Assert.Empty(excel.Items);
        Assert.Equal(2, excel.Sections.Count);
        Assert.Equal("Básico", excel.Sections[0].Name);
        Assert.Equal("Avançado", excel.Sections[1].Name);
        Assert.Equal(16, excel.Sections[0].Items.Count);
        Assert.Equal(25, excel.Sections[1].Items.Count);
    }

    [Fact]
    public void Word_is_single_group_with_eight_actions()
    {
        var wordGroups = ActionPaletteCatalog.Groups.Where(g => g.Name == "Word").ToList();
        Assert.Single(wordGroups);
        var word = wordGroups[0];
        Assert.Empty(word.Sections);
        Assert.Equal(8, word.Items.Count);
    }

    [Fact]
    public void Pdf_is_single_group_with_five_actions()
    {
        var pdfGroups = ActionPaletteCatalog.Groups.Where(g => g.Name == "PDF").ToList();
        Assert.Single(pdfGroups);
        var pdf = pdfGroups[0];
        Assert.Empty(pdf.Sections);
        Assert.Equal(5, pdf.Items.Count);
    }

    [Fact]
    public void All_palette_groups_and_sections_start_collapsed()
    {
        foreach (var group in ActionPaletteCatalog.Groups)
        {
            Assert.False(group.DefaultExpanded, $"group `{group.Name}` must start collapsed");
            foreach (var section in group.Sections)
                Assert.False(section.DefaultExpanded, $"section `{group.Name}/{section.Name}` must start collapsed");
        }
    }

    [Fact]
    public void Variables_group_has_pad_subgroups_and_thirty_six_actions()
    {
        var groups = ActionPaletteCatalog.Groups.Where(g => g.Name == "Variáveis").ToList();
        Assert.Single(groups);
        var v = groups[0];
        Assert.Empty(v.Items);
        Assert.Equal(5, v.Sections.Count);
        Assert.Equal(36, v.Sections.Sum(s => s.Items.Count));
        Assert.Equal(3, v.Sections[0].Items.Count);
        Assert.Equal(17, v.Sections[1].Items.Count);
        Assert.Equal(2, v.Sections[2].Items.Count);
        Assert.Equal(12, v.Sections[3].Items.Count);
        Assert.Equal(2, v.Sections[4].Items.Count);
    }

    [Fact]
    public void Web_automation_group_matches_pad_sections()
    {
        var groups = ActionPaletteCatalog.Groups.Where(g => g.Name == "Automação do navegador").ToList();
        Assert.Single(groups);
        var web = groups[0];
        Assert.Empty(web.Items);
        Assert.Equal(3, web.Sections.Count);
        Assert.Equal(31, web.Sections.Sum(s => s.Items.Count));
    }
}
