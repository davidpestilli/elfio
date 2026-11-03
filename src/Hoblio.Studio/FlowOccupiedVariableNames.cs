using Hoblio.Model;

namespace Hoblio.Studio;

public static class FlowOccupiedVariableNames
{
    public static HashSet<string> Collect(IEnumerable<Node> nodes, IEnumerable<string>? seedNames = null)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (seedNames is not null)
        {
            foreach (var name in seedNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                    set.Add(name.Trim());
            }
        }

        foreach (var node in nodes)
        {
            if (node.Action is { } action)
            {
                foreach (var v in action.ProducedVariables())
                {
                    if (!string.IsNullOrWhiteSpace(v))
                        set.Add(v.Trim());
                }
            }

            if (node.Kind == NodeKind.ForEach && node.Control is ForEachControlKind fe
                && !string.IsNullOrWhiteSpace(fe.ItemVariable))
                set.Add(fe.ItemVariable.Trim());

            if (node.Kind == NodeKind.Repeat && node.Control is RepeatControlKind rp
                && !string.IsNullOrWhiteSpace(rp.IndexVariable))
                set.Add(rp.IndexVariable.Trim());
        }

        return set;
    }
}
