using Hoblio.Model;
using Hoblio.Model.Json;

namespace Hoblio.Runtime;

public static class FlowExecutor
{
    const int MaxNodeVisits = 10_000;

    sealed class LoopFrameForEach(string forEachId, List<string> items, string itemVar)
    {
        public string ForEachId { get; } = forEachId;
        public List<string> Items { get; } = items;
        public int Index { get; set; }
        public string ItemVar { get; } = itemVar;
    }

    sealed class LoopFrameWhile(string whileId)
    {
        public string WhileId { get; } = whileId;
    }

    sealed class LoopFrameRepeat(string repeatId, int times, string indexVar)
    {
        public string RepeatId { get; } = repeatId;
        public int Times { get; } = times;
        public int Index { get; set; }
        public string IndexVar { get; } = indexVar;
    }

    public static RunOutcome RunProject(
        FlowProject project,
        string? projectRoot = null,
        Action<RunEvent>? onEvent = null,
        CancellationToken cancellationToken = default)
    {
        GraphValidator.ValidateProject(project);
        RunCancel.Reset();
        using var browser = new BrowserContext();
        return RunInner(project, projectRoot, browser, onEvent ?? (_ => { }), cancellationToken);
    }

    static RunOutcome RunInner(
        FlowProject project,
        string? projectRoot,
        BrowserContext browser,
        Action<RunEvent> onEvent,
        CancellationToken cancellationToken)
    {
        var events = new List<RunEvent>();
        void Emit(RunEvent e)
        {
            events.Add(e);
            onEvent(e);
        }

        Emit(new RunEvent.RunStarted(project.Manifest.Name));

        var nodes = project.Flow.Nodes.ToDictionary(n => n.Id);
        var routes = BuildRoutes(project.Flow.Edges);
        var runVars = RunVariables.SeedFromProject(project.Variables);
        var loopFrames = new List<object>();

        var current = project.Flow.Start;
        var visits = 0;

        while (true)
        {
            if (RunCancel.IsRequested || cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException("run cancelled by user");

            visits++;
            if (visits > MaxNodeVisits)
                throw new InvalidOperationException($"execution exceeded {MaxNodeVisits} node visits (possible infinite loop)");

            if (!nodes.TryGetValue(current, out var node))
                throw new InvalidOperationException($"node `{current}` missing");

            Emit(new RunEvent.NodeEnter(node.Id));
            string? routed = null;

            switch (node.Kind)
            {
                case NodeKind.Start:
                    break;
                case NodeKind.End:
                    Emit(new RunEvent.NodeExit(node.Id));
                    Emit(new RunEvent.RunFinished(node.Id));
                    return new RunOutcome { Events = events, LastNodeId = node.Id };
                case NodeKind.Action:
                {
                    if (node.Action is null)
                        throw new InvalidOperationException("action node without action");
                    var summary = ExecuteWithPolicy(node.Action, node.ErrorHandling, browser, runVars, projectRoot);
                    var variables = ActionExecutor.SnapshotVariables(node.Action, runVars);
                    Emit(new RunEvent.ActionExecuted(node.Id, summary, variables));
                    break;
                }
                case NodeKind.If:
                {
                    if (node.Control is not IfControlKind ifc)
                        throw new InvalidOperationException("if node has wrong control kind");
                    var cfg = ifc.AsIf();
                    var pass = ExpressionEvaluator.EvalIf(cfg, runVars);
                    var port = pass ? EdgePort.Then : EdgePort.Else;
                    var summary = $"if {cfg.Left} {CompareLabel(cfg.Operator)} {cfg.Right} => {port.AsString()}";
                    Emit(new RunEvent.ActionExecuted(node.Id, summary, null));
                    routed = RouteTo(routes, node.Id, port);
                    break;
                }
                case NodeKind.ForEach:
                {
                    if (node.Control is not ForEachControlKind fec)
                        throw new InvalidOperationException("for_each node has wrong control kind");
                    var cfg = fec.AsForEach();
                    var returning = loopFrames.FindLastIndex(f => f is LoopFrameForEach ff && ff.ForEachId == node.Id);
                    EdgePort port;
                    if (returning >= 0)
                    {
                        var frame = (LoopFrameForEach)loopFrames[returning];
                        frame.Index++;
                        if (frame.Index >= frame.Items.Count)
                        {
                            loopFrames.RemoveAt(returning);
                            port = EdgePort.After;
                        }
                        else
                        {
                            runVars[frame.ItemVar] = frame.Items[frame.Index];
                            port = EdgePort.Body;
                        }
                    }
                    else
                    {
                        var items = ExpressionEvaluator.ListFromVars(cfg.ListVariable, runVars);
                        if (items.Count == 0)
                            port = EdgePort.After;
                        else
                        {
                            runVars[cfg.ItemVariable] = items[0];
                            loopFrames.Add(new LoopFrameForEach(node.Id, items, cfg.ItemVariable));
                            port = EdgePort.Body;
                        }
                    }
                    Emit(new RunEvent.ActionExecuted(node.Id, $"for_each `{cfg.ListVariable}` -> port {port.AsString()}", null));
                    routed = RouteTo(routes, node.Id, port);
                    break;
                }
                case NodeKind.WhileLoop:
                {
                    if (node.Control is not WhileControlKind wc)
                        throw new InvalidOperationException("while_loop node has wrong control kind");
                    var cfg = wc.AsIf();
                    var active = loopFrames.Any(f => f is LoopFrameWhile w && w.WhileId == node.Id);
                    var pass = ExpressionEvaluator.EvalIf(cfg, runVars);
                    EdgePort port;
                    if (pass)
                    {
                        if (!active)
                            loopFrames.Add(new LoopFrameWhile(node.Id));
                        port = EdgePort.Body;
                    }
                    else if (active)
                    {
                        loopFrames.RemoveAll(f => f is LoopFrameWhile w && w.WhileId == node.Id);
                        port = EdgePort.After;
                    }
                    else
                        port = EdgePort.After;
                    Emit(new RunEvent.ActionExecuted(node.Id, $"while {cfg.Left} {CompareLabel(cfg.Operator)} {cfg.Right} => {port.AsString()}", null));
                    routed = RouteTo(routes, node.Id, port);
                    break;
                }
                case NodeKind.Repeat:
                {
                    if (node.Control is not RepeatControlKind rc)
                        throw new InvalidOperationException("repeat node has wrong control kind");
                    var cfg = rc.AsRepeat();
                    var returning = loopFrames.FindLastIndex(f => f is LoopFrameRepeat rr && rr.RepeatId == node.Id);
                    EdgePort port = EdgePort.After;
                    if (returning >= 0)
                    {
                        var frame = (LoopFrameRepeat)loopFrames[returning];
                        frame.Index++;
                        if (frame.Index >= frame.Times)
                        {
                            loopFrames.RemoveAt(returning);
                            port = EdgePort.After;
                        }
                        else
                        {
                            runVars[frame.IndexVar] = frame.Index.ToString();
                            port = EdgePort.Body;
                        }
                    }
                    else
                    {
                        var timesRaw = ExpressionEvaluator.ResolveOperand(cfg.Times, runVars);
                        var timesF = ExpressionEvaluator.ParseNumber(timesRaw);
                        if (timesF < 0)
                            throw new InvalidOperationException("repeat times must be >= 0");
                        var times = (int)Math.Round(timesF);
                        if (times == 0)
                            port = EdgePort.After;
                        else
                        {
                            runVars[cfg.IndexVariable] = "0";
                            loopFrames.Add(new LoopFrameRepeat(node.Id, times, cfg.IndexVariable));
                            port = EdgePort.Body;
                        }
                    }
                    Emit(new RunEvent.ActionExecuted(node.Id, $"repeat `{cfg.Times}` -> port {port.AsString()}", null));
                    routed = RouteTo(routes, node.Id, port);
                    break;
                }
                case NodeKind.NextLoop:
                {
                    if (loopFrames.Count == 0)
                        throw new InvalidOperationException("next_loop outside a loop");
                    var frame = loopFrames[^1];
                    routed = frame switch
                    {
                        LoopFrameForEach fe => NextForEach(fe, loopFrames, routes, runVars),
                        LoopFrameWhile wh => wh.WhileId,
                        LoopFrameRepeat rp => NextRepeat(rp, loopFrames, routes, runVars),
                        _ => throw new InvalidOperationException("next_loop frame mismatch"),
                    };
                    Emit(new RunEvent.ActionExecuted(node.Id, "next_loop", null));
                    break;
                }
                case NodeKind.ExitLoop:
                {
                    if (loopFrames.Count == 0)
                        throw new InvalidOperationException("exit_loop outside a loop");
                    var frame = loopFrames[^1];
                    loopFrames.RemoveAt(loopFrames.Count - 1);
                    var loopId = frame switch
                    {
                        LoopFrameForEach fe => fe.ForEachId,
                        LoopFrameWhile wh => wh.WhileId,
                        LoopFrameRepeat rp => rp.RepeatId,
                        _ => throw new InvalidOperationException("exit_loop frame mismatch"),
                    };
                    routed = RouteTo(routes, loopId, EdgePort.After);
                    Emit(new RunEvent.ActionExecuted(node.Id, "exit_loop", null));
                    break;
                }
            }

            Emit(new RunEvent.NodeExit(node.Id));
            current = routed ?? RouteTo(routes, current, EdgePort.Main);
        }
    }

    static string NextForEach(LoopFrameForEach frame, List<object> loopFrames, Dictionary<(string, EdgePort), string> routes, RunVariables runVars)
    {
        frame.Index++;
        if (frame.Index >= frame.Items.Count)
        {
            var idx = loopFrames.FindLastIndex(f => f is LoopFrameForEach fe && fe.ForEachId == frame.ForEachId);
            loopFrames.RemoveAt(idx);
            return RouteTo(routes, frame.ForEachId, EdgePort.After);
        }
        runVars[frame.ItemVar] = frame.Items[frame.Index];
        return RouteTo(routes, frame.ForEachId, EdgePort.Body);
    }

    static string NextRepeat(LoopFrameRepeat frame, List<object> loopFrames, Dictionary<(string, EdgePort), string> routes, RunVariables runVars)
    {
        frame.Index++;
        if (frame.Index >= frame.Times)
        {
            var idx = loopFrames.FindLastIndex(f => f is LoopFrameRepeat rr && rr.RepeatId == frame.RepeatId);
            loopFrames.RemoveAt(idx);
            return RouteTo(routes, frame.RepeatId, EdgePort.After);
        }
        runVars[frame.IndexVar] = frame.Index.ToString();
        return RouteTo(routes, frame.RepeatId, EdgePort.Body);
    }

    static string ExecuteWithPolicy(ActionKind action, ErrorHandling? policy, BrowserContext browser, RunVariables vars, string? projectRoot)
    {
        policy ??= new ErrorHandling();
        var attempts = policy.Attempts();
        var delayMs = (int)(policy.RetryDelayMs ?? 500);
        Exception? last = null;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return ActionExecutor.Execute(action, browser, vars, projectRoot);
            }
            catch (Exception ex) when (attempt < attempts)
            {
                last = ex;
                Thread.Sleep(delayMs);
            }
            catch (Exception ex) when (policy.ContinuesOnFailure())
            {
                return $"failed (continued): {RunErrors.ContinueSummary(ex)}";
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }
        throw last ?? new InvalidOperationException("action failed");
    }

    static Dictionary<(string From, EdgePort Port), string> BuildRoutes(IEnumerable<Edge> edges)
    {
        var map = new Dictionary<(string, EdgePort), string>();
        foreach (var edge in edges)
            map[(edge.From, edge.ResolvedPort())] = edge.To;
        return map;
    }

    static string RouteTo(Dictionary<(string, EdgePort), string> routes, string from, EdgePort port)
    {
        if (routes.TryGetValue((from, port), out var to))
            return to;
        throw new InvalidOperationException($"node `{from}` has no `{port.AsString()}` outgoing edge");
    }

    static string CompareLabel(CompareOperator op) => op switch
    {
        CompareOperator.Eq => "==",
        CompareOperator.Ne => "!=",
        CompareOperator.Contains => "contains",
        CompareOperator.Gt => ">",
        CompareOperator.Gte => ">=",
        CompareOperator.Lt => "<",
        CompareOperator.Lte => "<=",
        _ => op.ToString(),
    };
}
