using System.Text.Json;
using Hoblio.Model;
using Hoblio.Runtime;

namespace Hoblio.Runtime.Tests;

public sealed class SwitchBehaviorTests
{
    static FlowProject Project(FlowGraph graph, Dictionary<string, string>? vars = null)
    {
        var variables = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (vars is not null)
        {
            foreach (var (k, v) in vars)
                variables[k] = JsonSerializer.SerializeToElement(v);
        }

        return new FlowProject
        {
            Manifest = new FlowManifest { Name = "switch-test", Version = "1" },
            Flow = graph,
            Variables = variables,
        };
    }

    [Fact]
    public void Switch_routes_first_matching_case_contains()
    {
        var graph = new FlowGraph
        {
            Start = "start",
            Nodes =
            [
                new Node { Id = "start", Kind = NodeKind.Start },
                new Node
                {
                    Id = "sw",
                    Kind = NodeKind.Switch,
                    Control = new SwitchControlKind { Expression = "{text}", IgnoreCase = true },
                },
                new Node
                {
                    Id = "c_login",
                    Kind = NodeKind.Case,
                    Control = new CaseControlKind { Value = "acesso", Operator = CompareOperator.Contains },
                },
                new Node
                {
                    Id = "c_otp",
                    Kind = NodeKind.Case,
                    Control = new CaseControlKind { Value = "Código", Operator = CompareOperator.Contains },
                },
                new Node { Id = "def", Kind = NodeKind.CaseDefault, Control = new CaseDefaultControlKind() },
                new Node { Id = "a_login", Kind = NodeKind.Action, Action = new LogAction { Message = "login" } },
                new Node { Id = "a_otp", Kind = NodeKind.Action, Action = new LogAction { Message = "otp" } },
                new Node { Id = "a_def", Kind = NodeKind.Action, Action = new LogAction { Message = "default" } },
                new Node { Id = "end", Kind = NodeKind.End },
            ],
            Edges =
            [
                new Edge { From = "start", To = "sw" },
                new Edge { From = "sw", To = "c_login", Port = "then" },
                new Edge { From = "c_login", To = "a_login", Port = "then" },
                new Edge { From = "c_login", To = "c_otp", Port = "else" },
                new Edge { From = "c_otp", To = "a_otp", Port = "then" },
                new Edge { From = "c_otp", To = "def", Port = "else" },
                new Edge { From = "def", To = "a_def" },
                new Edge { From = "a_login", To = "end" },
                new Edge { From = "a_otp", To = "end" },
                new Edge { From = "a_def", To = "end" },
            ],
        };

        var outcomeLogin = FlowExecutor.RunProject(Project(graph, new() { ["text"] = "Tela de acesso" }));
        Assert.Contains(outcomeLogin.Events.OfType<RunEvent.ActionExecuted>(), e => e.Summary.Contains("login"));

        var outcomeOtp = FlowExecutor.RunProject(Project(graph, new() { ["text"] = "Digite o Código" }));
        Assert.Contains(outcomeOtp.Events.OfType<RunEvent.ActionExecuted>(), e => e.Summary.Contains("otp"));

        var outcomeDef = FlowExecutor.RunProject(Project(graph, new() { ["text"] = "outro" }));
        Assert.Contains(outcomeDef.Events.OfType<RunEvent.ActionExecuted>(), e => e.Summary.Contains("default"));
    }
}
