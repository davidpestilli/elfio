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
    public void Web_automation_group_matches_pad_sections()
    {
        var groups = ActionPaletteCatalog.Groups.Where(g => g.Name == "Automação do navegador").ToList();
        Assert.Single(groups);
        var web = groups[0];
        Assert.Empty(web.Items);
        Assert.Equal(3, web.Sections.Count);
        Assert.Equal(29, web.Sections.Sum(s => s.Items.Count));
    }
}
