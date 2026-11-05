using Hoblio.Model;

namespace Hoblio.Model.Tests;

public sealed class WebAutomationActionCoverageTests
{
    [Fact]
    public void All_palette_types_have_module_action_id()
    {
        Assert.Equal(29, WebAutomationActionTypes.All.Count);
        foreach (var type in WebAutomationActionTypes.All)
        {
            Assert.True(WebAutomationActionTypes.IsWebAutomationType(type));
            Assert.False(string.IsNullOrWhiteSpace(WebAutomationActionTypes.ModuleActionId(type)));
        }
    }
}
