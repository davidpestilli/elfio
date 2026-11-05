using Hoblio.Model;

namespace Hoblio.Model.Tests;

public sealed class ExcelActionCoverageTests
{
    [Fact]
    public void All_palette_types_have_module_action_id()
    {
        Assert.Equal(41, ExcelActionTypes.All.Count);
        foreach (var type in ExcelActionTypes.All)
        {
            Assert.True(ExcelActionTypes.IsExcelType(type));
            Assert.False(string.IsNullOrWhiteSpace(ExcelActionTypes.ModuleActionId(type)));
        }
    }
}
