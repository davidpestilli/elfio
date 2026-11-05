using Hoblio.Model;

namespace Hoblio.Model.Tests;

public sealed class VariablesActionCoverageTests
{
    [Fact]
    public void All_palette_types_have_module_action_id()
    {
        Assert.Equal(31, VariablesActionTypes.All.Count);
        foreach (var type in VariablesActionTypes.All)
        {
            Assert.True(VariablesActionTypes.IsVariablesType(type));
            Assert.False(string.IsNullOrWhiteSpace(VariablesActionTypes.ModuleActionId(type)));
        }
    }
}
