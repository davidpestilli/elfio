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
}
