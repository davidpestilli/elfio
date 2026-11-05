using Hoblio.Model;

namespace Hoblio.Model.Tests;

public sealed class WordActionCoverageTests
{
    [Fact]
    public void All_palette_types_have_module_action_id()
    {
        Assert.Equal(8, WordActionTypes.All.Count);
        foreach (var type in WordActionTypes.All)
        {
            Assert.True(WordActionTypes.IsWordType(type));
            Assert.False(string.IsNullOrWhiteSpace(WordActionTypes.ModuleActionId(type)));
        }
    }
}
