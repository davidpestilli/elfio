using Hoblio.Model;
using Hoblio.Studio;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    string? _producerRenameError;

    public string? ProducerRenameError
    {
        get => _producerRenameError;
        private set
        {
            _producerRenameError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasProducerRenameError));
        }
    }

    public bool HasProducerRenameError => !string.IsNullOrEmpty(ProducerRenameError);

    public bool NodeProducesVariableName(Node node, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        name = name.Trim();
        if (node.Action is { } action)
        {
            foreach (var v in action.ProducedVariables())
            {
                if (string.Equals(v, name, StringComparison.Ordinal))
                    return true;
            }
        }

        if (node.Kind == NodeKind.ForEach && node.Control is ForEachControlKind fe
            && string.Equals(fe.ItemVariable.Trim(), name, StringComparison.Ordinal))
            return true;
        if (node.Kind == NodeKind.Repeat && node.Control is RepeatControlKind rp
            && string.Equals(rp.IndexVariable.Trim(), name, StringComparison.Ordinal))
            return true;
        return false;
    }

    void ApplyProducerRenameOr(Node node, Action applyChange, string? oldName, string newName, bool allowEmptyProducer = false)
    {
        oldName = oldName?.Trim();
        newName = newName.Trim();
        if (!EnsureValidProducerName(node.Id, newName, allowEmptyProducer))
        {
            RefreshAllInspector();
            return;
        }

        if (oldName is { Length: > 0 } && newName != oldName && NodeProducesVariableName(node, oldName))
        {
            if (!TryApplyProducerRename(node.Id, oldName, newName, applyChange))
            {
                RefreshAllInspector();
                return;
            }
            return;
        }

        applyChange();
        CommitActionEdit(node);
    }

    bool EnsureValidProducerName(string nodeId, string newName, bool allowEmpty)
    {
        PersistCanvasToActiveGraph();
        var (nodes, _, _) = ActiveGraphSlice();
        var error = FlowProducerValidation.ValidateProducerName(nodes, newName, nodeId, allowEmpty);
        ProducerRenameError = error;
        return error is null;
    }

    void CommitForEachWithOptionalProducerRename(string? oldItemVar, ForEachControl next)
    {
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.ForEach)
            return;
        var newItem = next.ItemVariable.Trim();
        oldItemVar = oldItemVar?.Trim();
        if (oldItemVar is { Length: > 0 } && newItem != oldItemVar && NodeProducesVariableName(node, oldItemVar))
        {
            if (!TryApplyProducerRename(node.Id, oldItemVar, newItem, () =>
                    node.Control = new ForEachControlKind { ListVariable = next.ListVariable, ItemVariable = next.ItemVariable }))
                return;
            return;
        }

        if (!EnsureValidProducerName(node.Id, next.ItemVariable, allowEmpty: false))
        {
            RefreshAllInspector();
            return;
        }

        node.Control = new ForEachControlKind { ListVariable = next.ListVariable, ItemVariable = next.ItemVariable };
        MarkDirty();
        ReloadFlowVariableProducers();
        RebuildTemplateVariableNames();
        RefreshAllInspector();
    }

    void CommitRepeatWithOptionalProducerRename(string? oldIndexVar, RepeatControl next)
    {
        var node = GetSelectedNode();
        if (node?.Kind != NodeKind.Repeat)
            return;
        var newIndex = next.IndexVariable.Trim();
        oldIndexVar = oldIndexVar?.Trim();
        if (oldIndexVar is { Length: > 0 } && newIndex != oldIndexVar && NodeProducesVariableName(node, oldIndexVar))
        {
            if (!TryApplyProducerRename(node.Id, oldIndexVar, newIndex, () =>
                    node.Control = new RepeatControlKind { Times = next.Times, IndexVariable = next.IndexVariable }))
                return;
            return;
        }

        if (!EnsureValidProducerName(node.Id, next.IndexVariable, allowEmpty: false))
        {
            RefreshAllInspector();
            return;
        }

        node.Control = new RepeatControlKind { Times = next.Times, IndexVariable = next.IndexVariable };
        MarkDirty();
        ReloadFlowVariableProducers();
        RebuildTemplateVariableNames();
        RefreshAllInspector();
    }

    bool TryApplyProducerRename(string nodeId, string oldName, string newName, Action applyLocalChange)
    {
        newName = newName.Trim();
        if (!EnsureValidProducerName(nodeId, newName, allowEmpty: false))
            return false;

        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();

        PushHistory();
        var node = nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node is null)
            return false;

        applyLocalChange();
        var refs = FlowVariableRename.ApplyReferences(nodes, oldName, newName);
        CommitGraphSlice(nodes, edges);
        RebuildCanvas();
        ReloadFlowVariableProducers();
        RefreshAllInspector();
        SelectSingleNode(Nodes.FirstOrDefault(n => n.Id == nodeId));
        SetStatus(refs > 0
            ? $"Variável `{oldName}` renomeada para `{newName}` ({refs} nó(s) atualizado(s))."
            : $"Variável produtora renomeada para `{newName}`.");
        return true;
    }
}
