using System.Text.RegularExpressions;
using Hoblio.Model;

namespace Hoblio.Studio;

/// <summary>Operações de subfluxo embutido (paridade com hoblio-studio/subflowEditor.ts).</summary>
public static class SubflowDocumentOps
{
    static readonly Regex ValidId = new("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.CultureInvariant);

    public static bool IsValidSubflowId(string id) =>
        !string.IsNullOrWhiteSpace(id) && id.Trim() == id && ValidId.IsMatch(id);

    public static int PatchRunFlowIds(IList<Node> nodes, string oldId, string? newId)
    {
        var updated = 0;
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.Action is not RunFlowAction rf || rf.FlowId != oldId)
                continue;
            node.Action = new RunFlowAction
            {
                FlowId = newId,
                Path = rf.Path,
                Inputs = rf.Inputs,
                Outputs = rf.Outputs,
                ShareBrowser = rf.ShareBrowser,
                VariableScope = rf.VariableScope,
            };
            updated++;
        }
        return updated;
    }

    public sealed record RenameResult(bool Ok, string? Error, int UpdatedRunFlowNodes);

    public static RenameResult TryRename(FlowGraph graph, string oldId, string newId)
    {
        if (!IsValidSubflowId(newId))
            return new(false, "Use letras, números e _; comece com letra ou _.", 0);
        if (oldId == newId)
            return new(true, null, 0);
        if (graph.Subflows is null || !graph.Subflows.TryGetValue(oldId, out var bundle))
            return new(false, $"Subfluxo \"{oldId}\" não encontrado.", 0);
        if (graph.Subflows.ContainsKey(newId))
            return new(false, $"Subfluxo \"{newId}\" já existe.", 0);

        graph.Subflows.Remove(oldId);
        graph.Subflows[newId] = bundle;
        var updated = PatchRunFlowIds(graph.Nodes, oldId, newId);
        if (graph.Subflows.Count == 0)
            graph.Subflows = null;
        return new(true, null, updated);
    }

    public sealed record DeleteResult(bool Ok, string? Error, int ClearedRunFlowNodes);

    public static DeleteResult TryDelete(FlowGraph graph, string subflowId)
    {
        if (graph.Subflows is null || !graph.Subflows.Remove(subflowId))
            return new(false, $"Subfluxo \"{subflowId}\" não encontrado.", 0);
        if (graph.Subflows.Count == 0)
            graph.Subflows = null;
        var cleared = PatchRunFlowIds(graph.Nodes, subflowId, null);
        return new(true, null, cleared);
    }
}
