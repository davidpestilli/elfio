using System.Text.Json;
using Hoblio.Model;
using Hoblio.Model.Json;
using Hoblio.Serde;

namespace Hoblio.Model.Tests;

public class LinkedProjectManifestTests
{
    const string ManifestJson = """
    {
      "name": "eproc-superflow",
      "version": "0.1.0",
      "linked_projects": {
        "login": {
          "path": "../login-eproc",
          "description": "Login no eproc",
          "exports": ["ScreenText", "EprocUrl"]
        },
        "peticionar": {
          "path": "../eproc-peticionamento",
          "flow": "main",
          "requires": ["ScreenText"],
          "exports": ["PetitionNumber"]
        }
      }
    }
    """;

    [Fact]
    public void Manifest_reads_linked_projects()
    {
        var manifest = JsonSerializer.Deserialize<FlowManifest>(ManifestJson, HoblioJson.Options)!;

        Assert.NotNull(manifest.LinkedProjects);
        Assert.Equal(2, manifest.LinkedProjects!.Count);

        var login = manifest.LinkedProjects["login"];
        Assert.Equal("../login-eproc", login.Path);
        Assert.Equal(["ScreenText", "EprocUrl"], login.Exports!);

        var peticao = manifest.LinkedProjects["peticionar"];
        Assert.Equal("main", peticao.Flow);
        Assert.Equal(["ScreenText"], peticao.Requires!);
    }

    [Fact]
    public void Manifest_writes_linked_projects_without_nulls()
    {
        var manifest = JsonSerializer.Deserialize<FlowManifest>(ManifestJson, HoblioJson.Options)!;
        var json = JsonSerializer.Serialize(manifest, HoblioJson.Options);

        Assert.Contains("\"linked_projects\"", json);
        Assert.Contains("\"exports\"", json);
        Assert.DoesNotContain("\"requires\": null", json);
    }

    [Fact]
    public void Save_project_preserves_linked_projects()
    {
        var project = new FlowProject
        {
            Manifest = new FlowManifest
            {
                Name = "roundtrip",
                Version = "0.1.0",
                LinkedProjects = new Dictionary<string, LinkedProject>(StringComparer.Ordinal)
                {
                    ["login"] = new LinkedProject
                    {
                        Path = "../login-eproc",
                        Exports = ["ScreenText"],
                    },
                },
            },
            Flow = new FlowGraph
            {
                Start = "start",
                Nodes =
                [
                    new Node { Id = "start", Kind = NodeKind.Start },
                    new Node { Id = "end", Kind = NodeKind.End },
                ],
                Edges = [new Edge { From = "start", To = "end", Port = "main" }],
            },
        };

        var dir = Path.Combine(Path.GetTempPath(), "elfio-save-linked-" + Guid.NewGuid().ToString("N"));
        try
        {
            ProjectLoader.SaveProject(dir, project);
            var reloaded = ProjectLoader.LoadProject(dir);

            Assert.NotNull(reloaded.Manifest.LinkedProjects);
            Assert.Equal("../login-eproc", reloaded.Manifest.LinkedProjects!["login"].Path);
            Assert.Equal(["ScreenText"], reloaded.Manifest.LinkedProjects["login"].Exports!);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Run_flow_with_project_requires_declared_link()
    {
        var project = new FlowProject
        {
            Manifest = new FlowManifest
            {
                Name = "no-links",
                Version = "0.1.0",
            },
            Flow = new FlowGraph
            {
                Start = "start",
                Nodes =
                [
                    new Node { Id = "start", Kind = NodeKind.Start },
                    new Node
                    {
                        Id = "rf",
                        Kind = NodeKind.Action,
                        Action = new RunFlowAction { Project = "login" },
                    },
                    new Node { Id = "end", Kind = NodeKind.End },
                ],
                Edges =
                [
                    new Edge { From = "start", To = "rf", Port = "main" },
                    new Edge { From = "rf", To = "end", Port = "main" },
                ],
            },
        };

        var ex = Assert.Throws<ValidationException>(() => GraphValidator.ValidateProject(project));
        Assert.Equal(ValidationErrorKind.UnknownLinkedProject, ex.Kind);
    }
}
