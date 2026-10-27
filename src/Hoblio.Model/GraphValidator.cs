namespace Hoblio.Model;

public enum ValidationErrorKind
{
    StartMissing,
    WrongStartNode,
    DuplicateNodeId,
    UnknownNode,
    ActionNodeMissingAction,
    ControlNodeMissingControl,
    NonActionHasAction,
    NonControlHasControl,
    StartHasAction,
    EndHasOutgoing,
    Unreachable,
    MissingEnd,
    DuplicateOutgoingPort,
    MissingOutgoingPort,
    InvalidPortForNode,
    CycleWithoutForEach,
    LoopControlOutsideForEach,
    InvalidListVariable,
    InvalidVariableName,
    DuplicateVariableProducer,
    SetVariableNameMissing,
    InvalidRetryCount,
}

public sealed class ValidationException(ValidationErrorKind kind, string message) : Exception(message)
{
    public ValidationErrorKind Kind { get; } = kind;
}

public static class GraphValidator
{
    const uint MaxActionRetries = 50;

    public static void ValidateProject(FlowProject project)
    {
        ValidateGraph(project.Flow);
        ValidateForEachListNames(project.Flow);
        ValidateLoopControls(project.Flow);
        ValidateVariableProducers(project.Flow);
        ValidateErrorHandling(project.Flow);
    }

    public static void ValidateGraph(FlowGraph graph)
    {
        var byId = new Dictionary<string, Node>();
        foreach (var node in graph.Nodes)
        {
            if (!byId.TryAdd(node.Id, node))
                throw new ValidationException(ValidationErrorKind.DuplicateNodeId, $"duplicate node id `{node.Id}`");
        }

        if (!byId.TryGetValue(graph.Start, out var start))
            throw new ValidationException(ValidationErrorKind.StartMissing, $"start node `{graph.Start}` not found in nodes");

        if (start.Kind != NodeKind.Start)
            throw new ValidationException(ValidationErrorKind.WrongStartNode, $"node `{graph.Start}` has kind Start but is not the graph start id");

        if (start.Action is not null || start.Control is not null)
            throw new ValidationException(ValidationErrorKind.StartHasAction, $"start node `{graph.Start}` must not have an action");

        var hasEnd = false;
        foreach (var node in graph.Nodes)
        {
            if (node.Kind == NodeKind.Start && node.Id != graph.Start)
                throw new ValidationException(ValidationErrorKind.WrongStartNode, $"node `{node.Id}` has kind Start but is not the graph start id");
            if (node.Kind == NodeKind.End)
                hasEnd = true;
            if (node.Kind == NodeKind.Action && node.Action is null)
                throw new ValidationException(ValidationErrorKind.ActionNodeMissingAction, $"action node `{node.Id}` has no action");
            if (node.Kind is NodeKind.If or NodeKind.ForEach or NodeKind.WhileLoop or NodeKind.Repeat && node.Control is null)
                throw new ValidationException(ValidationErrorKind.ControlNodeMissingControl, $"control node `{node.Id}` has no control config");
            if (node.Action is not null && node.Kind != NodeKind.Action)
                throw new ValidationException(ValidationErrorKind.NonActionHasAction, $"node `{node.Id}` has action but is not an action node");
            if (node.Control is not null && node.Kind is not (NodeKind.If or NodeKind.ForEach or NodeKind.WhileLoop or NodeKind.Repeat))
                throw new ValidationException(ValidationErrorKind.NonControlHasControl, $"node `{node.Id}` has control but is not a control node");
        }

        if (!hasEnd)
            throw new ValidationException(ValidationErrorKind.MissingEnd, "graph has no End node");

        var outgoing = new Dictionary<string, List<(EdgePort Port, string To)>>();
        foreach (var edge in graph.Edges)
        {
            if (!byId.ContainsKey(edge.From))
                throw new ValidationException(ValidationErrorKind.UnknownNode, $"edge references unknown node `{edge.From}`");
            if (!byId.ContainsKey(edge.To))
                throw new ValidationException(ValidationErrorKind.UnknownNode, $"edge references unknown node `{edge.To}`");

            var fromNode = byId[edge.From];
            var port = edge.ResolvedPort();
            if (!PortAllowed(fromNode.Kind, port))
                throw new ValidationException(ValidationErrorKind.InvalidPortForNode, $"node `{edge.From}` must not use port `{port.AsString()}`");

            if (!outgoing.ContainsKey(edge.From))
                outgoing[edge.From] = [];
            outgoing[edge.From].Add((port, edge.To));
        }

        foreach (var (nodeId, edges) in outgoing)
        {
            var seen = new HashSet<EdgePort>();
            foreach (var (port, _) in edges)
            {
                if (!seen.Add(port))
                    throw new ValidationException(ValidationErrorKind.DuplicateOutgoingPort, $"node `{nodeId}` has duplicate outgoing port `{port.AsString()}`");
            }
        }

        foreach (var node in graph.Nodes)
        {
            if (node.Kind == NodeKind.End && outgoing.ContainsKey(node.Id))
                throw new ValidationException(ValidationErrorKind.EndHasOutgoing, $"end node `{node.Id}` must not have outgoing edges");
            if (ValidateRequiredPorts(node, outgoing.GetValueOrDefault(node.Id)) is { } err)
                throw err;
        }

        var reachable = ReachableFrom(graph.Start, outgoing);
        foreach (var node in graph.Nodes)
        {
            if (!reachable.Contains(node.Id))
                throw new ValidationException(ValidationErrorKind.Unreachable, $"node `{node.Id}` is unreachable from start");
        }

        if (CycleWithoutForEach(graph, byId) is { } cycle)
            throw new ValidationException(ValidationErrorKind.CycleWithoutForEach, $"cycle detected without a loop node (for_each / while_loop / repeat) (nodes: {cycle})");
    }

    static void ValidateErrorHandling(FlowGraph graph)
    {
        foreach (var node in graph.Nodes)
        {
            if (node.ErrorHandling is not { } policy)
                continue;
            if (policy.RetryCount > MaxActionRetries)
                throw new ValidationException(ValidationErrorKind.InvalidRetryCount, $"node `{node.Id}` retry_count {policy.RetryCount} exceeds maximum allowed");
        }
    }

    static bool IsValidVariableName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        if (!char.IsAsciiLetter(name[0]) && name[0] != '_')
            return false;
        return name.Skip(1).All(c => char.IsAsciiLetterOrDigit(c) || c == '_');
    }

    static void ValidateVariableProducers(FlowGraph graph)
    {
        var producers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes)
        {
            if (node.Action is { } action)
            {
                if (action is SetVariableAction { Variable: var sv } && string.IsNullOrWhiteSpace(sv))
                    throw new ValidationException(ValidationErrorKind.SetVariableNameMissing, $"node `{node.Id}` set_variable requires a non-empty variable name");
                if (action is FileReadAction { Variable: var fv } && string.IsNullOrWhiteSpace(fv))
                    throw new ValidationException(ValidationErrorKind.SetVariableNameMissing, $"node `{node.Id}` set_variable requires a non-empty variable name");
                if (action is ShowGetUserInputAction { UserInputVariable: var u, ButtonPressedVariable: var b }
                    && (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(b)))
                    throw new ValidationException(ValidationErrorKind.SetVariableNameMissing, $"node `{node.Id}` set_variable requires a non-empty variable name");
                if (action is ListAppendAction { ListVariable: var lv } && string.IsNullOrWhiteSpace(lv))
                    throw new ValidationException(ValidationErrorKind.InvalidListVariable, $"for_each node `{node.Id}` has invalid list variable name ``");
                if (action is ListClearAction { ListVariable: var lc } && string.IsNullOrWhiteSpace(lc))
                    throw new ValidationException(ValidationErrorKind.InvalidListVariable, $"for_each node `{node.Id}` has invalid list variable name ``");

                foreach (var name in action.ProducedVariables())
                {
                    if (!IsValidVariableName(name))
                        throw new ValidationException(ValidationErrorKind.InvalidVariableName, $"node `{node.Id}` uses invalid variable name `{name}`");
                    if (producers.TryGetValue(name, out var prev))
                        throw new ValidationException(ValidationErrorKind.DuplicateVariableProducer, $"variable `{name}` is produced by both `{prev}` and `{node.Id}`");
                    producers[name] = node.Id;
                }
            }

            if (node.Kind == NodeKind.ForEach && node.Control is ForEachControlKind cfg)
            {
                var name = cfg.ItemVariable.Trim();
                if (string.IsNullOrEmpty(name) || !IsValidVariableName(name))
                    throw new ValidationException(ValidationErrorKind.InvalidVariableName, $"node `{node.Id}` uses invalid variable name `{cfg.ItemVariable}`");
                if (producers.TryGetValue(name, out var prev))
                    throw new ValidationException(ValidationErrorKind.DuplicateVariableProducer, $"variable `{name}` is produced by both `{prev}` and `{node.Id}`");
                producers[name] = node.Id;
            }

            if (node.Kind == NodeKind.Repeat && node.Control is RepeatControlKind rc)
            {
                var name = rc.IndexVariable.Trim();
                if (string.IsNullOrEmpty(name) || !IsValidVariableName(name))
                    throw new ValidationException(ValidationErrorKind.InvalidVariableName, $"node `{node.Id}` uses invalid variable name `{rc.IndexVariable}`");
                if (producers.TryGetValue(name, out var prev))
                    throw new ValidationException(ValidationErrorKind.DuplicateVariableProducer, $"variable `{name}` is produced by both `{prev}` and `{node.Id}`");
                producers[name] = node.Id;
            }
        }
    }

    static void ValidateForEachListNames(FlowGraph graph)
    {
        foreach (var node in graph.Nodes)
        {
            if (node.Kind != NodeKind.ForEach || node.Control is not ForEachControlKind cfg)
                continue;
            var name = cfg.ListVariable.Trim();
            if (string.IsNullOrEmpty(name) || !IsValidVariableName(name))
                throw new ValidationException(ValidationErrorKind.InvalidListVariable, $"for_each node `{node.Id}` has invalid list variable name `{cfg.ListVariable}`");
        }
    }

    static void ValidateLoopControls(FlowGraph graph)
    {
        var outgoing = graph.Edges
            .GroupBy(e => e.From)
            .ToDictionary(g => g.Key, g => g.Select(e => (e.ResolvedPort(), e.To)).ToList());

        var bodyUnion = new HashSet<string>();
        foreach (var node in graph.Nodes)
        {
            if (node.Kind is NodeKind.ForEach or NodeKind.WhileLoop or NodeKind.Repeat)
                bodyUnion.UnionWith(LoopBodyNodes(node.Id, outgoing));
        }

        foreach (var node in graph.Nodes)
        {
            if (node.Kind is NodeKind.NextLoop or NodeKind.ExitLoop && !bodyUnion.Contains(node.Id))
                throw new ValidationException(ValidationErrorKind.LoopControlOutsideForEach, $"node `{node.Id}` must be inside the body of a loop (for_each, while_loop, or repeat)");
        }
    }

    static HashSet<string> LoopBodyNodes(string loopId, Dictionary<string, List<(EdgePort, string To)>> outgoing)
    {
        var bodyStart = outgoing.GetValueOrDefault(loopId)?
            .FirstOrDefault(t => t.Item1 == EdgePort.Body).To;
        if (bodyStart is null)
            return [];

        var seen = new HashSet<string>();
        var stack = new Stack<string>();
        stack.Push(bodyStart);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!seen.Add(id))
                continue;
            if (id == loopId)
                continue;
            if (!outgoing.TryGetValue(id, out var edges))
                continue;
            foreach (var (port, to) in edges)
            {
                if (id == loopId && port == EdgePort.After)
                    continue;
                stack.Push(to);
            }
        }
        return seen;
    }

    static string? CycleWithoutForEach(FlowGraph graph, Dictionary<string, Node> byId)
    {
        var adj = new Dictionary<string, List<string>>();
        foreach (var edge in graph.Edges)
        {
            if (!adj.ContainsKey(edge.From))
                adj[edge.From] = [];
            adj[edge.From].Add(edge.To);
            if (edge.From == edge.To && byId.TryGetValue(edge.From, out var node) && !IsLoopHeader(node.Kind))
                return edge.From;
        }

        var index = 0;
        var stack = new List<string>();
        var onStack = new HashSet<string>();
        var indices = new Dictionary<string, int>();
        var lowlink = new Dictionary<string, int>();
        var sccNodes = new List<List<string>>();

        void StrongConnect(string v)
        {
            indices[v] = index;
            lowlink[v] = index;
            index++;
            stack.Add(v);
            onStack.Add(v);

            if (adj.TryGetValue(v, out var nexts))
            {
                foreach (var w in nexts)
                {
                    if (!indices.ContainsKey(w))
                    {
                        StrongConnect(w);
                        lowlink[v] = Math.Min(lowlink[v], lowlink[w]);
                    }
                    else if (onStack.Contains(w))
                    {
                        lowlink[v] = Math.Min(lowlink[v], indices[w]);
                    }
                }
            }

            if (lowlink[v] == indices[v])
            {
                var component = new List<string>();
                while (true)
                {
                    var w = stack[^1];
                    stack.RemoveAt(stack.Count - 1);
                    onStack.Remove(w);
                    component.Add(w);
                    if (w == v)
                        break;
                }
                sccNodes.Add(component);
            }
        }

        foreach (var node in graph.Nodes)
        {
            if (!indices.ContainsKey(node.Id))
                StrongConnect(node.Id);
        }

        foreach (var component in sccNodes)
        {
            if (component.Count <= 1)
                continue;
            var hasLoop = component.Any(id => byId.TryGetValue(id, out var n) && IsLoopHeader(n.Kind));
            if (!hasLoop)
                return string.Join(", ", component);
        }
        return null;
    }

    static bool IsLoopHeader(NodeKind kind) =>
        kind is NodeKind.ForEach or NodeKind.WhileLoop or NodeKind.Repeat;

    static bool PortAllowed(NodeKind kind, EdgePort port) => kind switch
    {
        NodeKind.Start or NodeKind.Action or NodeKind.NextLoop or NodeKind.ExitLoop => port == EdgePort.Main,
        NodeKind.End => false,
        NodeKind.If => port is EdgePort.Then or EdgePort.Else,
        NodeKind.ForEach or NodeKind.WhileLoop or NodeKind.Repeat => port is EdgePort.Body or EdgePort.After,
        _ => false,
    };

    static ValidationException? ValidateRequiredPorts(Node node, List<(EdgePort, string)>? edges)
    {
        var ports = edges?.Select(e => e.Item1).ToHashSet() ?? [];
        EdgePort[]? need = node.Kind switch
        {
            NodeKind.If => [EdgePort.Then, EdgePort.Else],
            NodeKind.ForEach or NodeKind.WhileLoop or NodeKind.Repeat => [EdgePort.Body, EdgePort.After],
            _ => null,
        };
        if (need is null)
            return null;
        foreach (var port in need)
        {
            if (!ports.Contains(port))
                return new ValidationException(ValidationErrorKind.MissingOutgoingPort, $"node `{node.Id}` is missing required outgoing port `{port.AsString()}`");
        }
        return null;
    }

    static HashSet<string> ReachableFrom(string start, Dictionary<string, List<(EdgePort, string To)>> outgoing)
    {
        var seen = new HashSet<string>();
        var stack = new Stack<string>();
        stack.Push(start);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!seen.Add(id))
                continue;
            if (outgoing.TryGetValue(id, out var nexts))
            {
                foreach (var (_, to) in nexts)
                    stack.Push(to);
            }
        }
        return seen;
    }
}
