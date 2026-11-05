using Hoblio.Model;
using Hoblio.Serde;
using Hoblio.Studio;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class LinkedProjectOpsTests
{
    static string NewRoot() =>
        Path.Combine(Path.GetTempPath(), "elfio-linkops-" + Guid.NewGuid().ToString("N"));

    static string CreateProject(string root, string name)
    {
        var dir = Path.Combine(root, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "manifest.json"), $"{{\"name\":\"{name}\",\"version\":\"0.1.0\"}}");
        File.WriteAllText(Path.Combine(dir, "flow.json"), """
        {
          "start": "start",
          "nodes": [
            { "id": "start", "kind": "start", "label": "Start" },
            { "id": "end", "kind": "end", "label": "End" }
          ],
          "edges": [ { "from": "start", "to": "end", "port": "main" } ]
        }
        """);
        return dir;
    }

    [Fact]
    public void Suggest_id_sanitizes_folder_name()
    {
        Assert.Equal("login_eproc", LinkedProjectOps.SuggestId(@"C:\x\login eproc"));
        Assert.Equal("eproc-peticionamento", LinkedProjectOps.SuggestId(@"C:\x\eproc-peticionamento"));
        Assert.Equal("projeto", LinkedProjectOps.SuggestId(@"C:\x\..."));
    }

    [Fact]
    public void Sanitize_folder_name_drops_invalid_chars()
    {
        Assert.Equal("meu fluxo", LinkedProjectOps.SanitizeFolderName("  meu fluxo  "));
        Assert.Equal("ab", LinkedProjectOps.SanitizeFolderName("a\"<>|b"));
        Assert.Equal("", LinkedProjectOps.SanitizeFolderName("   "));
    }

    [Fact]
    public void Relative_path_is_normalized_to_forward_slash()
    {
        var root = NewRoot();
        try
        {
            var caller = CreateProject(root, "superflow");
            var target = CreateProject(root, "login-eproc");
            Assert.Equal("../login-eproc", LinkedProjectOps.RelativePath(caller, target));
            Assert.Equal("login-eproc", LinkedProjectOps.RelativePath(root, target));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Plan_rejects_folder_without_manifest_or_flow()
    {
        var root = NewRoot();
        try
        {
            var caller = CreateProject(root, "superflow");
            var notAProject = Path.Combine(root, "vazia");
            Directory.CreateDirectory(notAProject);

            Assert.Null(LinkedProjectOps.Plan(null, caller, notAProject));
            Assert.Null(LinkedProjectOps.Plan(null, caller, caller));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Plan_reuses_existing_link_for_same_folder()
    {
        var root = NewRoot();
        try
        {
            var caller = CreateProject(root, "superflow");
            var target = CreateProject(root, "login-eproc");
            var links = new Dictionary<string, LinkedProject>(StringComparer.Ordinal)
            {
                ["login"] = new LinkedProject { Path = "../login-eproc", Exports = ["ScreenText"] },
            };

            var planned = LinkedProjectOps.Plan(links, caller, target);

            Assert.NotNull(planned);
            Assert.Equal("login", planned!.Id);
            Assert.Equal("../login-eproc", planned.RelativePath);
            Assert.Equal(["ScreenText"], planned.Link.Exports!);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Plan_generates_unique_id_when_name_already_used()
    {
        var root = NewRoot();
        try
        {
            var caller = CreateProject(root, "superflow");
            var first = CreateProject(root, "login");
            var second = Path.Combine(root, "outro", "login");
            Directory.CreateDirectory(second);
            File.Copy(Path.Combine(first, "manifest.json"), Path.Combine(second, "manifest.json"));
            File.Copy(Path.Combine(first, "flow.json"), Path.Combine(second, "flow.json"));

            var links = new Dictionary<string, LinkedProject>(StringComparer.Ordinal)
            {
                ["login"] = new LinkedProject { Path = "../login" },
            };

            var planned = LinkedProjectOps.Plan(links, caller, second);

            Assert.NotNull(planned);
            Assert.Equal("login_2", planned!.Id);
            Assert.Equal("../outro/login", planned.RelativePath);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Created_scaffold_is_a_valid_project_and_can_be_linked()
    {
        var root = NewRoot();
        try
        {
            var caller = CreateProject(root, "superflow");

            var target = LinkedProjectOps.CreateScaffold(root, "eproc-peticionamento");

            Assert.NotNull(target);
            Assert.True(LinkedProjectOps.IsProjectFolder(target!));
            ProjectLoader.ValidateProjectDir(target!);

            var planned = LinkedProjectOps.Plan(null, caller, target!);
            Assert.NotNull(planned);
            Assert.Equal("eproc-peticionamento", planned!.Id);
            Assert.Equal("../eproc-peticionamento", planned.RelativePath);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Apply_creates_dictionary_and_survives_manifest_roundtrip()
    {
        var root = NewRoot();
        try
        {
            var caller = CreateProject(root, "superflow");
            CreateProject(root, "login-eproc");
            var project = ProjectLoader.LoadProject(caller);

            var planned = LinkedProjectOps.Plan(project.Manifest.LinkedProjects, caller, Path.Combine(root, "login-eproc"));
            Assert.NotNull(planned);
            LinkedProjectOps.Apply(project.Manifest, planned!);

            Assert.Equal(["login-eproc"], LinkedProjectOps.Ids(project.Manifest));

            ProjectLoader.SaveProject(caller, project);
            var reloaded = ProjectLoader.LoadProject(caller);
            Assert.Equal("../login-eproc", reloaded.Manifest.LinkedProjects!["login-eproc"].Path);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
