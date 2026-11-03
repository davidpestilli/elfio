using Hoblio.Model;

namespace Hoblio.Studio;

public static class FlowProducerValidation
{
    public static string? ValidateProducerName(IEnumerable<Node> nodes, string? name, string nodeId, bool allowEmpty = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            return allowEmpty ? null : "Nome da variável produtora é obrigatório.";

        var trimmed = name.Trim();
        if (!ProjectVariablesEditor.IsValidVariableName(trimmed))
            return "Nome inválido (letra/_ inicial, depois alfanumérico/_).";

        if (FlowVariableRename.WouldDuplicateProducer(nodes, trimmed, nodeId))
            return $"Variável \"{trimmed}\" já é produzida por outro nó.";

        return null;
    }
}
