using Hoblio.Model;

namespace Hoblio.Model.Tests;

public sealed class ActionCatalogTests
{
    [Fact]
    public void Describe_browser_navigate_uses_web_automation_module()
    {
        var d = ActionCatalog.Describe(new WebAutomationAction
        {
            Operation = WebAutomationActionTypes.Navigate,
            Url = "https://example.com",
        });
        Assert.Equal("WebAutomation", d.Module);
        Assert.Equal("LaunchChromeBase", d.ActionId);
    }

    [Fact]
    public void CollectModulesFromGraph_includes_controls_and_actions()
    {
        var graph = new FlowGraph
        {
            Start = "s",
            Nodes =
            [
                new Node { Id = "s", Kind = NodeKind.Start },
                new Node
                {
                    Id = "a",
                    Kind = NodeKind.Action,
                    Action = new LogAction { Message = "hi" },
                },
                new Node
                {
                    Id = "i",
                    Kind = NodeKind.If,
                    Control = new IfControlKind { Left = "x", Operator = CompareOperator.Eq, Right = "1" },
                },
                new Node { Id = "e", Kind = NodeKind.End },
            ],
            Edges = [],
        };
        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ActionCatalog.CollectModulesFromGraph(graph, modules);
        Assert.Contains("System", modules);
        Assert.Contains("Conditionals", modules);
        Assert.DoesNotContain(modules, m => m.Equals("FlowControl", StringComparison.OrdinalIgnoreCase));
    }
}
