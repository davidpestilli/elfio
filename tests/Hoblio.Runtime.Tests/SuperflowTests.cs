using System.Text.Json;
using Hoblio.Model;
using Hoblio.Model.Json;
using Hoblio.Runtime;
using Hoblio.Serde;
using Hoblio.TestSupport;

namespace Hoblio.Runtime.Tests;

/// <summary>
/// Composição de fluxos principais ("superfluxo"): o nó <c>run_flow</c> com <c>project</c> resolve um
/// fluxo de outro projeto dentro do MESMO run (mesma sessão de browser, mesma tabela de variáveis).
/// </summary>
public class SuperflowTests
{
    const string SimpleFlowJson = """
    {
      "start": "start",
      "nodes": [
        { "id": "start", "kind": "start", "label": "Start" },
        { "id": "end", "kind": "end", "label": "End" }
      ],
      "edges": [
        { "from": "start", "to": "end", "port": "main" }
      ]
    }
    """;

    const string ChildManifestJson = """
    { "name": "child", "version": "0.1.0" }
    """;

    [Fact]
    public void Superflow_demo_chains_two_main_flows_and_inherits_state()
    {
        var path = ExamplePaths.Resolve("superflow-demo");
        var project = ProjectLoader.LoadProject(path);

        var outcome = FlowExecutor.RunProject(project, path);

        Assert.Equal("end", outcome.LastNodeId);

        var logs = outcome.Events.OfType<RunEvent.ActionExecuted>().Select(e => e.Summary).ToList();
        Assert.Contains(logs, s => s.Contains("sessao pronta em `Painel do Advogado`"));
        Assert.Contains(logs, s => s.Contains("assumindo a tela `Painel do Advogado`"));
        Assert.Contains(logs, s => s.Contains("peticionamento protocolado: PET-2026-0001"));
        Assert.Contains(logs, s => s.Contains("superfluxo concluido: PET-2026-0001 na tela Painel do Advogado"));

        var startedFlows = outcome.Events.OfType<RunEvent.FlowStarted>().Select(e => e.FlowRef).ToList();
        Assert.Contains("login:entry", startedFlows);
        Assert.Contains("peticionar:entry", startedFlows);
    }

    [Fact]
    public void Superflow_isolated_scope_merges_only_declared_outputs()
    {
        var childFlow = """
        {
          "start": "start",
          "nodes": [
            { "id": "start", "kind": "start", "label": "Start" },
            { "id": "set_kept", "kind": "action", "label": "Set", "action": { "type": "set_variable", "variable": "kept", "value": "from-child" } },
            { "id": "set_leak", "kind": "action", "label": "Set", "action": { "type": "set_variable", "variable": "leak", "value": "should-not-leak" } },
            { "id": "end", "kind": "end", "label": "End" }
          ],
          "edges": [
            { "from": "start", "to": "set_kept", "port": "main" },
            { "from": "set_kept", "to": "set_leak", "port": "main" },
            { "from": "set_leak", "to": "end", "port": "main" }
          ]
        }
        """;

        var outcome = RunSuperflow("""
        {
          "name": "isolated-demo",
          "version": "0.1.0",
          "linked_projects": { "child": { "path": "../child" } }
        }
        """, """
        {
          "start": "start",
          "nodes": [
            { "id": "start", "kind": "start", "label": "Start" },
            {
              "id": "rf",
              "kind": "action",
              "label": "Child",
              "action": { "type": "run_flow", "project": "child", "variable_scope": "isolated", "outputs": ["kept"] }
            },
            { "id": "log", "kind": "action", "label": "Log", "action": { "type": "log", "message": "kept={kept} leak={leak}" } },
            { "id": "end", "kind": "end", "label": "End" }
          ],
          "edges": [
            { "from": "start", "to": "rf", "port": "main" },
            { "from": "rf", "to": "log", "port": "main" },
            { "from": "log", "to": "end", "port": "main" }
          ],
          "variables": { "kept": "", "leak": "" }
        }
        """, childFlowJson: childFlow);

        var logs = outcome.Events.OfType<RunEvent.ActionExecuted>().Select(e => e.Summary).ToList();
        Assert.Contains(logs, s => s.Contains("kept=from-child") && s.Contains("leak="));
        Assert.DoesNotContain(logs, s => s.Contains("leak=should-not-leak"));
    }

    [Fact]
    public void Run_flow_project_must_be_declared_in_manifest()
    {
        var dir = CreateSuperflow("""
        {
          "name": "undeclared",
          "version": "0.1.0"
        }
        """, """
        {
          "start": "start",
          "nodes": [
            { "id": "start", "kind": "start", "label": "Start" },
            { "id": "rf", "kind": "action", "label": "Child", "action": { "type": "run_flow", "project": "ghost" } },
            { "id": "end", "kind": "end", "label": "End" }
          ],
          "edges": [
            { "from": "start", "to": "rf", "port": "main" },
            { "from": "rf", "to": "end", "port": "main" }
          ]
        }
        """);

        try
        {
            var project = ProjectLoader.LoadProject(dir);
            var ex = Assert.Throws<ValidationException>(() => GraphValidator.ValidateProject(project));
            Assert.Equal(ValidationErrorKind.UnknownLinkedProject, ex.Kind);
            Assert.Contains("ghost", ex.Message);
        }
        finally
        {
            DeleteRoot(dir);
        }
    }

    [Fact]
    public void Linked_project_path_must_exist_on_disk()
    {
        var dir = CreateSuperflow("""
        {
          "name": "broken-link",
          "version": "0.1.0",
          "linked_projects": { "ghost": { "path": "../does-not-exist" } }
        }
        """, SimpleFlowJson);

        try
        {
            var project = ProjectLoader.LoadProject(dir);
            var ex = Assert.Throws<ValidationException>(() => LinkedProjectValidator.Validate(project, dir));
            Assert.Equal(ValidationErrorKind.InvalidLinkedProject, ex.Kind);
            Assert.Contains("ghost", ex.Message);
        }
        finally
        {
            DeleteRoot(dir);
        }
    }

    [Fact]
    public void Linked_project_cycle_is_rejected()
    {
        var root = Path.Combine(Path.GetTempPath(), "elfio-linked-cycle-" + Guid.NewGuid().ToString("N"));
        var a = Path.Combine(root, "a");
        var b = Path.Combine(root, "b");
        Directory.CreateDirectory(a);
        Directory.CreateDirectory(b);
        try
        {
            WriteProject(a, """
            { "name": "a", "version": "0.1.0", "linked_projects": { "b": { "path": "../b" } } }
            """, SimpleFlowJson);
            WriteProject(b, """
            { "name": "b", "version": "0.1.0", "linked_projects": { "a": { "path": "../a" } } }
            """, SimpleFlowJson);

            var project = ProjectLoader.LoadProject(a);
            var ex = Assert.Throws<ValidationException>(() => LinkedProjectValidator.Validate(project, a));
            Assert.Equal(ValidationErrorKind.InvalidLinkedProject, ex.Kind);
            Assert.Contains("cycle", ex.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Linked_project_exports_satisfy_template_references()
    {
        var dir = CreateSuperflow("""
        {
          "name": "exports",
          "version": "0.1.0",
          "linked_projects": { "login": { "path": "../child", "exports": ["ScreenText"] } }
        }
        """, """
        {
          "start": "start",
          "nodes": [
            { "id": "start", "kind": "start", "label": "Start" },
            { "id": "rf", "kind": "action", "label": "Login", "action": { "type": "run_flow", "project": "login" } },
            { "id": "log", "kind": "action", "label": "Log", "action": { "type": "log", "message": "tela {ScreenText}" } },
            { "id": "end", "kind": "end", "label": "End" }
          ],
          "edges": [
            { "from": "start", "to": "rf", "port": "main" },
            { "from": "rf", "to": "log", "port": "main" },
            { "from": "log", "to": "end", "port": "main" }
          ]
        }
        """);

        try
        {
            var project = ProjectLoader.LoadProject(dir);
            GraphValidator.ValidateProject(project);
            TemplateReferenceValidator.Validate(project);
        }
        finally
        {
            DeleteRoot(dir);
        }
    }

    [Fact]
    public void Linked_project_entry_follows_manifest_entry_flow()
    {
        var root = NewRoot("elfio-linked-entry-");
        var parent = Path.Combine(root, "parent");
        var child = Path.Combine(root, "child");
        Directory.CreateDirectory(parent);
        Directory.CreateDirectory(Path.Combine(child, "flows"));
        try
        {
            WriteProject(parent, """
            {
              "name": "entry-superflow",
              "version": "0.1.0",
              "linked_projects": { "child": { "path": "../child" } }
            }
            """, """
            {
              "start": "start",
              "nodes": [
                { "id": "start", "kind": "start", "label": "Start" },
                { "id": "rf", "kind": "action", "label": "Child", "action": { "type": "run_flow", "project": "child" } },
                { "id": "end", "kind": "end", "label": "End" }
              ],
              "edges": [
                { "from": "start", "to": "rf", "port": "main" },
                { "from": "rf", "to": "end", "port": "main" }
              ]
            }
            """);
            WriteProject(child, """
            {
              "name": "child",
              "version": "0.1.0",
              "entry_flow": "flows/main.json"
            }
            """, SimpleFlowJson);
            File.WriteAllText(Path.Combine(child, "flows", "main.json"), """
            {
              "start": "start",
              "nodes": [
                { "id": "start", "kind": "start", "label": "Start" },
                { "id": "log", "kind": "action", "label": "Log", "action": { "type": "log", "message": "child entry ran" } },
                { "id": "end", "kind": "end", "label": "End" }
              ],
              "edges": [
                { "from": "start", "to": "log", "port": "main" },
                { "from": "log", "to": "end", "port": "main" }
              ]
            }
            """);

            var project = ProjectLoader.LoadProject(parent);
            var outcome = FlowExecutor.RunProject(project, parent);

            var logs = outcome.Events.OfType<RunEvent.ActionExecuted>().Select(e => e.Summary).ToList();
            Assert.Contains(logs, s => s.Contains("child entry ran"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Run_flow_action_serializes_project_field()
    {
        var action = new RunFlowAction { Project = "login", FlowId = "entry", Outputs = ["ScreenText"] };
        var json = JsonSerializer.Serialize<ActionKind>(action, HoblioJson.Options);
        Assert.Contains("\"project\": \"login\"", json);

        var back = JsonSerializer.Deserialize<ActionKind>(
            """{ "type": "run_flow", "project": "login", "flow_id": "entry" }""", HoblioJson.Options);
        var runFlow = Assert.IsType<RunFlowAction>(back);
        Assert.Equal("login", runFlow.Project);
        Assert.Equal("entry", runFlow.FlowId);
    }

    static void WriteProject(string dir, string manifestJson, string flowJson)
    {
        File.WriteAllText(Path.Combine(dir, "manifest.json"), manifestJson);
        File.WriteAllText(Path.Combine(dir, "flow.json"), flowJson);
    }

    /// <summary>Cria um superfluxo em pasta temporária com um projeto filho ao lado (<c>../child</c>).</summary>
    static string CreateSuperflow(
        string manifestJson,
        string flowJson,
        string? childManifestJson = null,
        string? childFlowJson = null)
    {
        var root = NewRoot("elfio-superflow-");
        var parent = Path.Combine(root, "parent");
        var child = Path.Combine(root, "child");
        Directory.CreateDirectory(parent);
        Directory.CreateDirectory(child);
        WriteProject(parent, manifestJson, flowJson);
        WriteProject(child, childManifestJson ?? ChildManifestJson, childFlowJson ?? SimpleFlowJson);
        return parent;
    }

    static RunOutcome RunSuperflow(
        string manifestJson,
        string flowJson,
        string? childManifestJson = null,
        string? childFlowJson = null)
    {
        var parent = CreateSuperflow(manifestJson, flowJson, childManifestJson, childFlowJson);
        var root = Path.GetDirectoryName(parent)!;
        try
        {
            var project = ProjectLoader.LoadProject(parent);
            return FlowExecutor.RunProject(project, parent);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    static string NewRoot(string prefix) =>
        Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));

    /// <summary>Remove a raiz temporária que contém <paramref name="projectDir"/> e o projeto filho irmão.</summary>
    static void DeleteRoot(string projectDir)
    {
        var root = Path.GetDirectoryName(projectDir);
        if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
