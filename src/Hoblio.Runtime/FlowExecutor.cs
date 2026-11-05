using Hoblio.Model;
using Hoblio.Model.Json;
using Hoblio.Serde;

namespace Hoblio.Runtime;

public static partial class FlowExecutor
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

    sealed class LoopFrameSwitch(string switchId, string value, bool ignoreCase)
    {
        public string SwitchId { get; } = switchId;
        public string Value { get; } = value;
        public bool IgnoreCase { get; } = ignoreCase;
    }

    public static RunOutcome RunProject(
        FlowProject project,
        string? projectRoot = null,
        Action<RunEvent>? onEvent = null,
        CancellationToken cancellationToken = default)
    {
        GraphValidator.ValidateProject(project);
        if (!string.IsNullOrWhiteSpace(projectRoot))
            LinkedProjectValidator.Validate(project, projectRoot);
        RunCancel.Reset();
        using var browser = new BrowserContext();
        using var excel = new ExcelContext();
        using var word = new WordContext();
        return RunInner(project, projectRoot, browser, excel, word, onEvent ?? (_ => { }), cancellationToken);
    }

    static RunOutcome RunInner(
        FlowProject project,
        string? projectRoot,
        BrowserContext browser,
        ExcelContext excel,
        WordContext word,
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

        var runVars = RunVariables.SeedFromProject(project.Variables);
        var flowCache = new ChildFlowCache();
        var visits = 0;
        var telemetry = new RunTelemetry();
        var endNode = ExecuteGraph(
            project.Flow,
            project.Flow,
            project.Manifest.Name,
            depth: 0,
            project.Manifest,
            projectRoot,
            browser,
            excel,
            word,
            runVars,
            [],
            flowCache,
            ref visits,
            telemetry,
            Emit,
            cancellationToken);
        Emit(new RunEvent.RunFinished(endNode));
        return new RunOutcome { Events = events, LastNodeId = endNode };
    }

    static string ExecuteGraph(
        FlowGraph graph,
        FlowGraph resolveGraph,
        string flowRef,
        int depth,
        FlowManifest manifest,
        string? projectRoot,
        BrowserContext browser,
        ExcelContext excel,
        WordContext word,
        RunVariables runVars,
        List<object> loopFrames,
        ChildFlowCache flowCache,
        ref int visits,
        RunTelemetry telemetry,
        Action<RunEvent> emit,
        CancellationToken cancellationToken)
    {
        if (depth >= ChildFlowResolver.MaxRunFlowDepth)
            throw new InvalidOperationException($"run_flow exceeds maximum depth {ChildFlowResolver.MaxRunFlowDepth}");

        if (depth > 0)
            GraphValidator.ValidateGraph(graph);

        var nodes = graph.Nodes.ToDictionary(n => n.Id);
        var routes = BuildRoutes(graph.Edges);
        var current = graph.Start;

        while (true)
        {
            if (RunCancel.IsRequested || cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException("run cancelled by user");

            visits++;
            if (visits > MaxNodeVisits)
                throw new InvalidOperationException($"execution exceeded {MaxNodeVisits} node visits (possible infinite loop)");

            if (!nodes.TryGetValue(current, out var node))
                throw new InvalidOperationException($"node `{current}` missing");

            emit(new RunEvent.NodeEnter(node.Id));
            string? routed = null;

            switch (node.Kind)
            {
                case NodeKind.Start:
                    break;
                case NodeKind.End:
                    emit(new RunEvent.NodeExit(node.Id));
                    return node.Id;
                case NodeKind.Action:
                {
                    if (node.Action is null)
                        throw new InvalidOperationException("action node without action");
                    string summary;
                    if (node.Action is RunFlowAction runFlow)
                    {
                        var resolved = ChildFlowResolver.Resolve(projectRoot, manifest, resolveGraph, runFlow, flowCache);
                        // Label do resolvedor: `subflow:x`, `flow_id`, caminho, ou `projeto:fluxo` para projeto vinculado.
                        var childLabel = resolved.Label;
                        var scope = runFlow.VariableScope ?? RunFlowVariableScope.Inherit;
                        emit(new RunEvent.FlowStarted(childLabel, node.Id));
                        string endNode;
                        if (scope == RunFlowVariableScope.Isolated)
                        {
                            var snapshot = new RunVariables();
                            foreach (var kv in runVars)
                                snapshot[kv.Key] = kv.Value;
                            var childVars = new RunVariables();
                            RunFlowSupport.ApplyIsolatedInputs(runFlow.Inputs, resolved.Variables, runVars, childVars);
                            endNode = ExecuteGraph(
                                resolved.Graph,
                                resolved.Graph,
                                childLabel,
                                depth + 1,
                                resolved.Manifest,
                                resolved.ProjectRoot,
                                browser,
                                excel,
                                word,
                                childVars,
                                [],
                                flowCache,
                                ref visits,
                                telemetry,
                                emit,
                                cancellationToken);
                            RunFlowSupport.MergeIsolatedOutputs(runVars, snapshot, childVars, runFlow.Outputs);
                        }
                        else
                        {
                            RunFlowSupport.ApplyInheritInputs(runFlow.Inputs, resolved.Variables, runVars);
                            endNode = ExecuteGraph(
                                resolved.Graph,
                                resolved.Graph,
                                childLabel,
                                depth + 1,
                                resolved.Manifest,
                                resolved.ProjectRoot,
                                browser,
                                excel,
                                word,
                                runVars,
                                [],
                                flowCache,
                                ref visits,
                                telemetry,
                                emit,
                                cancellationToken);
                        }
                        emit(new RunEvent.FlowFinished(childLabel, endNode));
                        summary = RunFlowSupport.Summary(childLabel, endNode, runFlow.Outputs, runVars);
                    }
                    else
                    {
                        var outcome = ExecuteWithPolicy(
                            node, flowRef, telemetry, emit,
                            node.Action, node.ErrorHandling, browser, excel, word, runVars, projectRoot);
                        summary = outcome.Summary;
                        EmitActionExecuted(
                            emit, telemetry, node, flowRef, summary,
                            ActionExecutor.SnapshotVariables(node.Action, runVars),
                            outcome.Status, outcome.InsideErrorHandling, outcome.ErrorCode);
                        break;
                    }
                    var variables = ActionExecutor.SnapshotVariables(node.Action, runVars);
                    EmitActionExecuted(emit, telemetry, node, flowRef, summary, variables);
                    break;
                }
                case NodeKind.If:
                {
                    if (node.Control is not IfControlKind)
                        throw new InvalidOperationException("if node has wrong control kind");
                    var cfg = node.Control.AsIf();
                    var pass = ExpressionEvaluator.EvalIf(cfg, runVars);
                    var port = pass ? EdgePort.Then : EdgePort.Else;
                    var summary = $"if {cfg.Left} {CompareLabel(cfg.Operator)} {cfg.Right} => {port.AsString()}";
                    EmitActionExecuted(emit, telemetry, node, flowRef, summary, null);
                    routed = RouteTo(routes, node.Id, port);
                    break;
                }
                case NodeKind.Switch:
                {
                    if (node.Control is not SwitchControlKind)
                        throw new InvalidOperationException("switch node has wrong control kind");
                    var cfg = node.Control.AsSwitch();
                    var value = ExpressionEvaluator.ResolveOperand(cfg.Expression, runVars);
                    loopFrames.Add(new LoopFrameSwitch(node.Id, value, cfg.IgnoreCase));
                    var summary = $"switch {cfg.Expression} => `{value}`";
                    EmitActionExecuted(emit, telemetry, node, flowRef, summary, null);
                    routed = RouteTo(routes, node.Id, EdgePort.Then);
                    break;
                }
                case NodeKind.Case:
                {
                    if (node.Control is not CaseControlKind)
                        throw new InvalidOperationException("case node has wrong control kind");
                    var frame = ActiveSwitchFrame(loopFrames)
                                ?? throw new InvalidOperationException($"case `{node.Id}` is not inside an active switch");
                    var cfg = node.Control.AsCase();
                    var right = ExpressionEvaluator.ResolveOperand(cfg.Value, runVars);
                    var pass = ExpressionEvaluator.EvalCompareValues(frame.Value, cfg.Operator, right, frame.IgnoreCase);
                    if (pass)
                        PopSwitchFrame(loopFrames, frame);
                    var port = pass ? EdgePort.Then : EdgePort.Else;
                    var summary = $"case {cfg.Operator.ToString().ToLowerInvariant()} `{cfg.Value}` => {port.AsString()}";
                    EmitActionExecuted(emit, telemetry, node, flowRef, summary, null);
                    routed = RouteTo(routes, node.Id, port);
                    break;
                }
                case NodeKind.CaseDefault:
                {
                    if (node.Control is not CaseDefaultControlKind)
                        throw new InvalidOperationException("case_default node has wrong control kind");
                    if (ActiveSwitchFrame(loopFrames) is { } frame)
                        PopSwitchFrame(loopFrames, frame);
                    EmitActionExecuted(emit, telemetry, node, flowRef, "case default", null);
                    break;
                }
                case NodeKind.ForEach:
                {
                    if (node.Control is not ForEachControlKind)
                        throw new InvalidOperationException("for_each node has wrong control kind");
                    var cfg = node.Control.AsForEach();
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
                    EmitActionExecuted(
                        emit, telemetry, node, flowRef,
                        $"for_each `{cfg.ListVariable}` -> port {port.AsString()}", null,
                        inputVariableNames: [cfg.ListVariable],
                        outputVariableNames: [cfg.ItemVariable]);
                    routed = RouteTo(routes, node.Id, port);
                    break;
                }
                case NodeKind.WhileLoop:
                {
                    if (node.Control is not WhileControlKind)
                        throw new InvalidOperationException("while_loop node has wrong control kind");
                    var cfg = node.Control.AsIf();
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
                    EmitActionExecuted(
                        emit, telemetry, node, flowRef,
                        $"while {cfg.Left} {CompareLabel(cfg.Operator)} {cfg.Right} => {port.AsString()}", null);
                    routed = RouteTo(routes, node.Id, port);
                    break;
                }
                case NodeKind.Repeat:
                {
                    if (node.Control is not RepeatControlKind)
                        throw new InvalidOperationException("repeat node has wrong control kind");
                    var cfg = node.Control.AsRepeat();
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
                    EmitActionExecuted(
                        emit, telemetry, node, flowRef,
                        $"repeat `{cfg.Times}` -> port {port.AsString()}", null,
                        outputVariableNames: [cfg.IndexVariable]);
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
                    EmitActionExecuted(emit, telemetry, node, flowRef, "next_loop", null);
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
                    EmitActionExecuted(emit, telemetry, node, flowRef, "exit_loop", null);
                    break;
                }
            }

            emit(new RunEvent.NodeExit(node.Id));
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

    readonly record struct ActionRunOutcome(string Summary, string Status, bool InsideErrorHandling, string? ErrorCode);

    static ActionRunOutcome ExecuteWithPolicy(
        Node node,
        string flowRef,
        RunTelemetry telemetry,
        Action<RunEvent> emit,
        ActionKind action,
        ErrorHandling? policy,
        BrowserContext browser,
        ExcelContext excel,
        WordContext word,
        RunVariables vars,
        string? projectRoot)
    {
        policy ??= new ErrorHandling();
        var attempts = policy.Attempts();
        var delayMs = (int)(policy.RetryDelayMs ?? 500);
        Exception? last = null;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                var summary = ActionExecutor.Execute(action, browser, excel, word, vars, projectRoot);
                return new ActionRunOutcome(summary, "Succeeded", false, null);
            }
            catch (Exception ex) when (attempt < attempts)
            {
                last = ex;
                EmitRepeatAction(emit, telemetry, node, flowRef);
                Thread.Sleep(delayMs);
            }
            catch (Exception ex) when (policy.ContinuesOnFailure())
            {
                return new ActionRunOutcome(
                    $"failed (continued): {RunErrors.ContinueSummary(ex)}",
                    "Continued",
                    true,
                    null);
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }
        throw last ?? new InvalidOperationException("action failed");
    }

    static void EmitRepeatAction(
        Action<RunEvent> emit,
        RunTelemetry telemetry,
        Node node,
        string flowRef)
    {
        emit(new RunEvent.ActionExecuted(
            node.Id,
            "repeat action",
            null,
            flowRef,
            telemetry.NextLogIndex(),
            telemetry.CurrentStepIndex,
            null,
            "RepeatAction",
            node.Label,
            "Succeeded",
            InsideErrorHandling: true));
    }

    static void EmitActionExecuted(
        Action<RunEvent> emit,
        RunTelemetry telemetry,
        Node node,
        string flowRef,
        string summary,
        Dictionary<string, string>? variables,
        string status = "Succeeded",
        bool insideErrorHandling = false,
        string? errorCode = null,
        IReadOnlyList<string>? inputVariableNames = null,
        IReadOnlyList<string>? outputVariableNames = null)
    {
        var descriptor = ActionCatalog.Describe(node);
        if (node.Action is not null && inputVariableNames is null && outputVariableNames is null)
        {
            var io = ActionCatalog.VariableIo(node.Action);
            inputVariableNames = io.Inputs;
            outputVariableNames = io.Outputs;
        }

        emit(new RunEvent.ActionExecuted(
            node.Id,
            summary,
            variables,
            flowRef,
            telemetry.NextLogIndex(),
            telemetry.NextStepIndex(),
            descriptor.Module,
            descriptor.ActionId,
            node.Label,
            status,
            insideErrorHandling,
            errorCode,
            inputVariableNames,
            outputVariableNames));
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

    static LoopFrameSwitch? ActiveSwitchFrame(List<object> loopFrames)
    {
        for (var i = loopFrames.Count - 1; i >= 0; i--)
        {
            if (loopFrames[i] is LoopFrameSwitch sw)
                return sw;
        }
        return null;
    }

    static void PopSwitchFrame(List<object> loopFrames, LoopFrameSwitch frame)
    {
        for (var i = loopFrames.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(loopFrames[i], frame))
            {
                loopFrames.RemoveAt(i);
                return;
            }
        }
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
