using Hoblio.Model;
using Hoblio.Serde;
using Hoblio.TestSupport;

namespace Hoblio.Model.Tests;

/// <summary>
/// Regressão de **fiação** dos ramos de modo de execução nos fluxos do eproc. O <c>if</c> testa
/// <c>{HoblioRunMode} eq standalone</c>, então o ramo <c>then</c> é o **individual**: caixa de
/// mensagem no login e <c>attach</c> no peticionamento. Com as portas trocadas o modal aparece no
/// meio de um superfluxo e o fluxo individual termina em silêncio — foi o que aconteceu na primeira
/// montagem, e nenhum dos dois comportamentos quebra a validação do grafo.
/// </summary>
public class ExampleBranchingTests
{
    [Fact]
    public void Login_eproc_standalone_branch_shows_message_and_superflow_branch_only_logs()
    {
        var project = ProjectLoader.LoadProject(ExamplePaths.Resolve("login-eproc"));

        var gate = Assert.IsType<IfControlKind>(Node(project, "if_run_mode").Control);
        Assert.Equal("{HoblioRunMode}", gate.Left);
        Assert.Equal("standalone", gate.Right);

        var individual = Chain(project, Target(project, "if_run_mode", EdgePort.Then));
        Assert.Contains(individual, n => n.Action is ShowMessageAction);

        var superflow = Chain(project, Target(project, "if_run_mode", EdgePort.Else));
        Assert.Contains(superflow, n => n.Action is LogAction);
        Assert.DoesNotContain(superflow, n => n.Action is ShowMessageAction);
    }

    [Fact]
    public void Petition_standalone_branch_attaches_and_superflow_branch_reuses_the_session()
    {
        var project = ProjectLoader.LoadProject(ExamplePaths.Resolve("eproc-peticionamento"));

        var gate = Assert.IsType<IfControlKind>(Node(project, "if_run_mode").Control);
        Assert.Equal("{HoblioRunMode}", gate.Left);
        Assert.Equal("standalone", gate.Right);

        var individual = Chain(project, Target(project, "if_run_mode", EdgePort.Then));
        var attach = Assert.Single(
            individual.Select(n => n.Action).OfType<WebAutomationAction>(),
            a => a.Type is WebAutomationActionTypes.AttachChrome or WebAutomationActionTypes.AttachEdge);
        Assert.Equal(9222UL, attach.DebuggingPort);

        var superflow = Chain(project, Target(project, "if_run_mode", EdgePort.Else));
        Assert.Contains(superflow, n => n.Action is RunFlowAction);
        Assert.DoesNotContain(superflow, n => n.Action is WebAutomationAction { Operation: WebAutomationActionTypes.AttachChrome });
    }

    static Node Node(FlowProject project, string id) =>
        project.Flow.Nodes.Single(n => n.Id == id);

    static string Target(FlowProject project, string from, EdgePort port) =>
        project.Flow.Edges.Single(e => e.From == from && e.ResolvedPort() == port).To;

    /// <summary>Nós alcançados seguindo só as arestas <c>main</c> a partir de <paramref name="startId"/>.</summary>
    static List<Node> Chain(FlowProject project, string startId, int maxHops = 30)
    {
        var byId = project.Flow.Nodes.ToDictionary(n => n.Id);
        var mainRoute = project.Flow.Edges
            .Where(e => e.ResolvedPort() == EdgePort.Main)
            .GroupBy(e => e.From)
            .ToDictionary(g => g.Key, g => g.First().To);

        var chain = new List<Node>();
        var current = startId;
        while (chain.Count < maxHops && byId.TryGetValue(current, out var node))
        {
            chain.Add(node);
            if (node.Kind == NodeKind.End || !mainRoute.TryGetValue(current, out var next))
                break;
            current = next;
        }
        return chain;
    }
}
