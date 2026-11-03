using Hoblio.Model;
using Hoblio.Studio;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class SubflowDocumentOpsTests
{
    static FlowGraph GraphWithSubflowAndRunFlow(string subflowId = "child") => new()
    {
        Start = "start",
        Nodes =
        [
            new Node { Id = "start", Kind = NodeKind.Start },
            new Node
            {
                Id = "call",
                Kind = NodeKind.Action,
                Action = new RunFlowAction { FlowId = subflowId },
            },
        ],
        Edges = [],
        Subflows = new Dictionary<string, SubflowBundle>(StringComparer.Ordinal)
        {
            [subflowId] = new SubflowBundle
            {
                Start = "sf_start",
                Nodes = [new Node { Id = "sf_start", Kind = NodeKind.Start }],
                Edges = [],
            },
        },
    };

    [Fact]
    public void Rename_updates_subflow_key_and_run_flow_nodes()
    {
        var graph = GraphWithSubflowAndRunFlow("old_id");
        var result = SubflowDocumentOps.TryRename(graph, "old_id", "new_id");
        Assert.True(result.Ok);
        Assert.Equal(1, result.UpdatedRunFlowNodes);
        Assert.True(graph.Subflows!.ContainsKey("new_id"));
        Assert.False(graph.Subflows.ContainsKey("old_id"));
        Assert.Equal("new_id", ((RunFlowAction)graph.Nodes[1].Action!).FlowId);
    }

    [Fact]
    public void Rename_rejects_invalid_id()
    {
        var graph = GraphWithSubflowAndRunFlow();
        var result = SubflowDocumentOps.TryRename(graph, "child", "9bad");
        Assert.False(result.Ok);
    }

    [Fact]
    public void Delete_removes_subflow_and_clears_run_flow_refs()
    {
        var graph = GraphWithSubflowAndRunFlow("gone");
        var result = SubflowDocumentOps.TryDelete(graph, "gone");
        Assert.True(result.Ok);
        Assert.Equal(1, result.ClearedRunFlowNodes);
        Assert.Null(graph.Subflows);
        Assert.Null(((RunFlowAction)graph.Nodes[1].Action!).FlowId);
    }
}
