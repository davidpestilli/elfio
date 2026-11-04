using Hoblio.Model;

namespace Hoblio.Studio;

static class NodeOutputPorts
{
    public static IReadOnlyList<EdgePort> ForKind(NodeKind kind) => kind switch
    {
        NodeKind.End => [],
        NodeKind.If => [EdgePort.Then, EdgePort.Else],
        NodeKind.ForEach or NodeKind.WhileLoop or NodeKind.Repeat => [EdgePort.Body, EdgePort.After],
        _ => [EdgePort.Main],
    };
}
