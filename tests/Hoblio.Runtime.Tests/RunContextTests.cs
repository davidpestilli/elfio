using Hoblio.Model;
using Hoblio.Runtime;
using Hoblio.Serde;

namespace Hoblio.Runtime.Tests;

/// <summary>
/// Variáveis de contexto publicadas pelo runtime (<see cref="BuiltInVariables"/>): o fluxo sabe se
/// está rodando sozinho (<c>standalone</c>) ou como etapa de um superfluxo (<c>superflow</c>) — e volta
/// a ver o próprio modo depois que o fluxo filho termina.
/// </summary>
public class RunContextTests
{
    static readonly string ModeProbeMessage =
        "modo={" + BuiltInVariables.RunMode + "} nivel={" + BuiltInVariables.RunDepth + "}";

    static readonly string ParentProbeMessage =
        "depois modo={" + BuiltInVariables.RunMode + "} nivel={" + BuiltInVariables.RunDepth + "}";

    [Fact]
    public void Root_run_reports_standalone_mode()
    {
        var dir = WriteProject(NewRoot("elfio-runmode-root-"), SimpleManifest("solo"), FlowWithLog(ModeProbeMessage));
        try
        {
            var logs = Logs(Run(dir));
            Assert.Contains(logs, s => s.Contains("modo=standalone nivel=0"));
        }
        finally
        {
            Delete(dir);
        }
    }

    [Fact]
    public void Linked_project_child_reports_superflow_mode_and_parent_restores()
    {
        var root = NewRoot("elfio-runmode-linked-");
        var parent = Path.Combine(root, "parent");
        var child = Path.Combine(root, "child");
        Directory.CreateDirectory(parent);
        Directory.CreateDirectory(child);
        try
        {
            WriteProject(child, SimpleManifest("child"), FlowWithLog(ModeProbeMessage));
            WriteProject(parent, LinkedManifest(), FlowCallingChild(extraAction: ParentProbeMessage));

            var logs = Logs(Run(parent));

            Assert.Contains(logs, s => s.Contains("modo=superflow nivel=1"));
            Assert.Contains(logs, s => s.Contains("depois modo=standalone nivel=0"));
        }
        finally
        {
            Delete(root);
        }
    }

    [Fact]
    public void Isolated_scope_child_also_reports_superflow_mode()
    {
        var root = NewRoot("elfio-runmode-isolated-");
        var parent = Path.Combine(root, "parent");
        var child = Path.Combine(root, "child");
        Directory.CreateDirectory(parent);
        Directory.CreateDirectory(child);
        try
        {
            WriteProject(child, SimpleManifest("child"), FlowWithLog(ModeProbeMessage));
            WriteProject(parent, LinkedManifest(), FlowCallingChild(extraAction: null, variableScope: "isolated"));

            var logs = Logs(Run(parent));

            Assert.Contains(logs, s => s.Contains("modo=superflow nivel=1"));
        }
        finally
        {
            Delete(root);
        }
    }

    /// <summary>Subfluxo é o mesmo projeto: o run continua individual para ele.</summary>
    [Fact]
    public void Embedded_subflow_keeps_caller_mode()
    {
        var dir = WriteProject(NewRoot("elfio-runmode-subflow-"), SimpleManifest("solo"), """
        {
          "start": "start",
          "nodes": [
            { "id": "start", "kind": "start", "label": "Start" },
            {
              "id": "rf",
              "kind": "action",
              "label": "Subfluxo",
              "action": { "type": "run_flow", "flow_id": "probe" }
            },
            { "id": "end", "kind": "end", "label": "End" }
          ],
          "edges": [
            { "from": "start", "to": "rf", "port": "main" },
            { "from": "rf", "to": "end", "port": "main" }
          ],
          "subflows": {
            "probe": {
              "start": "start",
              "nodes": [
                { "id": "start", "kind": "start", "label": "Start" },
                {
                  "id": "log_mode",
                  "kind": "action",
                  "label": "Log",
                  "action": { "type": "log", "message": "sub modo={HoblioRunMode} nivel={HoblioRunDepth}" }
                },
                { "id": "end", "kind": "end", "label": "End" }
              ],
              "edges": [
                { "from": "start", "to": "log_mode", "port": "main" },
                { "from": "log_mode", "to": "end", "port": "main" }
              ],
              "variables": {}
            }
          }
        }
        """);
        try
        {
            var logs = Logs(Run(dir));
            Assert.Contains(logs, s => s.Contains("sub modo=standalone nivel=0"));
        }
        finally
        {
            Delete(dir);
        }
    }

    static string LinkedManifest() => """
    {
      "name": "parent",
      "version": "0.1.0",
      "linked_projects": { "child": { "path": "../child" } }
    }
    """;

    /// <summary>Superfluxo que chama o projeto vinculado e, opcionalmente, registra o próprio modo depois.</summary>
    static string FlowCallingChild(string? extraAction, string variableScope = "inherit")
    {
        var probe = extraAction is null
            ? ""
            : $$"""
            {
              "id": "log_after",
              "kind": "action",
              "label": "Depois",
              "action": { "type": "log", "message": "{{extraAction}}" }
            },
            """;
        var probeEdge = extraAction is null
            ? """
            { "from": "rf", "to": "end", "port": "main" }
            """
            : """
            { "from": "rf", "to": "log_after", "port": "main" },
            { "from": "log_after", "to": "end", "port": "main" }
            """;

        return $$"""
        {
          "start": "start",
          "nodes": [
            { "id": "start", "kind": "start", "label": "Start" },
            {
              "id": "rf",
              "kind": "action",
              "label": "Filho",
              "action": { "type": "run_flow", "project": "child", "variable_scope": "{{variableScope}}" }
            },
            {{probe}}
            { "id": "end", "kind": "end", "label": "End" }
          ],
          "edges": [
            { "from": "start", "to": "rf", "port": "main" },
            {{probeEdge}}
          ]
        }
        """;
    }

    static string FlowWithLog(string message) => $$"""
    {
      "start": "start",
      "nodes": [
        { "id": "start", "kind": "start", "label": "Start" },
        { "id": "log_mode", "kind": "action", "label": "Log", "action": { "type": "log", "message": "{{message}}" } },
        { "id": "end", "kind": "end", "label": "End" }
      ],
      "edges": [
        { "from": "start", "to": "log_mode", "port": "main" },
        { "from": "log_mode", "to": "end", "port": "main" }
      ]
    }
    """;

    static string SimpleManifest(string name) => $$"""{ "name": "{{name}}", "version": "0.1.0" }""";

    static string WriteProject(string dir, string manifestJson, string flowJson)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "manifest.json"), manifestJson);
        File.WriteAllText(Path.Combine(dir, "flow.json"), flowJson);
        return dir;
    }

    static RunOutcome Run(string projectDir)
    {
        var project = ProjectLoader.LoadProject(projectDir);
        return FlowExecutor.RunProject(project, projectDir);
    }

    static List<string> Logs(RunOutcome outcome) =>
        outcome.Events.OfType<RunEvent.ActionExecuted>().Select(e => e.Summary).ToList();

    static string NewRoot(string prefix) => Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));

    static void Delete(string dir)
    {
        try
        {
            Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // best effort (temporário)
        }
    }
}
