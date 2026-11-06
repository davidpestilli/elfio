using System.CommandLine;
using System.Text;
using Hoblio.Runtime;
using Hoblio.Serde;

Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
Console.InputEncoding = Encoding.UTF8;

return await CliApp.RunAsync(args);

static class CliApp
{
    public static async Task<int> RunAsync(string[] args)
    {
        var root = BuildRoot();
        await root.InvokeAsync(args);
        return Environment.ExitCode == 0 ? 0 : Environment.ExitCode;
    }

    static RootCommand BuildRoot()
    {
        var root = new RootCommand("Hoblio flow runner (C# / WPF port)");
        var projectArg = new Argument<string>("project_dir", "Path to project folder (manifest.json + flow.json)");

        var runCmd = new Command("run", "Load a project folder and execute the flow from Start");
        runCmd.AddArgument(projectArg);
        var jsonlOpt = new Option<bool>("--jsonl", "Append structured run logs to `<project>/.hoblio/run.jsonl`");
        runCmd.AddOption(jsonlOpt);
        runCmd.SetHandler((string dir, bool jsonl) =>
        {
            try { RunProject(dir, jsonl); }
            catch (Exception ex) { Fail(ex); }
        }, projectArg, jsonlOpt);

        var validateCmd = new Command("validate", "Validate manifest, flow schema, and graph invariants");
        validateCmd.AddArgument(projectArg);
        var updateManifestOpt = new Option<bool>(
            "--update-manifest",
            "Refresh module_references and engine_version in manifest.json without changing flow.json");
        validateCmd.AddOption(updateManifestOpt);
        validateCmd.SetHandler((string dir, bool updateManifest) =>
        {
            try { ValidateProject(dir, updateManifest); }
            catch (Exception ex) { Fail(ex); }
        }, projectArg, updateManifestOpt);

        var browserCmd = new Command(
            "browser",
            "Open Chrome/Edge with remote debugging (CDP) on a dedicated profile, so a flow can attach to it");
        var engineOpt = new Option<string?>("--engine", "chrome (default) or edge");
        var portOpt = new Option<int>("--port", () => CdpBrowser.DefaultPort, "CDP port (default 9222)");
        var urlOpt = new Option<string?>("--url", "Page to open in the browser session");
        var profileOpt = new Option<string?>("--profile", "Profile directory (default %TEMP%\\elfio-cdp-profile)");
        browserCmd.AddOption(engineOpt);
        browserCmd.AddOption(portOpt);
        browserCmd.AddOption(urlOpt);
        browserCmd.AddOption(profileOpt);
        browserCmd.SetHandler(
            (string? engine, int port, string? url, string? profile) =>
            {
                try { OpenBrowser(engine, port, url, profile); }
                catch (Exception ex) { Fail(ex); }
            },
            engineOpt, portOpt, urlOpt, profileOpt);

        root.AddCommand(runCmd);
        root.AddCommand(validateCmd);
        root.AddCommand(browserCmd);
        return root;
    }

    static void Fail(Exception ex)
    {
        Console.Error.WriteLine($"Error: {RunErrors.CliMessage(ex)}");
        if (string.Equals(Environment.GetEnvironmentVariable("HOBLIO_DEBUG"), "1", StringComparison.Ordinal))
            Console.Error.WriteLine(ex);
        Environment.ExitCode = 1;
    }

    static void RunProject(string projectDir, bool jsonl)
    {
        if (jsonl)
            RunLogWriter.Enable(projectDir);
        var project = ProjectLoader.LoadProject(projectDir);
        var outcome = FlowExecutor.RunProject(project, projectDir, evt =>
        {
            if (evt is RunEvent.ActionExecuted ae)
                Console.WriteLine(ae.Summary);
            RunLogWriter.Write(evt);
        });
        Console.Error.WriteLine($"run complete: project={project.Manifest.Name} last_node={outcome.LastNodeId}");
    }

    static void ValidateProject(string projectDir, bool updateManifest)
    {
        ProjectLoader.ValidateProjectDir(projectDir);
        if (updateManifest)
        {
            var wrote = ProjectLoader.UpdateManifestCapabilities(projectDir);
            Console.Error.WriteLine(wrote
                ? $"manifest updated: {projectDir}"
                : $"manifest unchanged: {projectDir}");
        }
        Console.Error.WriteLine($"valid: {projectDir}");
    }

    /// <summary>
    /// Abre o navegador com CDP e reporta a sessão — o mesmo serviço do botão do Studio
    /// (<see cref="CdpBrowser"/>). O navegador fica aberto; o comando termina.
    /// </summary>
    static void OpenBrowser(string? engine, int port, string? url, string? profileDir)
    {
        var session = CdpBrowser.Launch(engine, port, url, profileDir);
        var reused = session.ReusedRunningBrowser
            ? " (sessão existente reaproveitada)"
            : "";
        Console.WriteLine(
            $"cdp: {session.BrowserLabel ?? session.Engine} em http://127.0.0.1:{session.Port}{reused} (pid {session.ProcessId})");
        Console.WriteLine($"perfil: {session.ProfileDir}");
        foreach (var tab in session.Tabs)
            Console.WriteLine($"aba: {tab.Title} [{tab.Url}]");
        Console.Error.WriteLine(
            $"deixe o navegador aberto, navegue/logue e rode o fluxo: "
            + $"o browser_attach_* procura a porta {session.Port}");
    }
}

static class RunLogWriter
{
    static StreamWriter? _writer;

    public static void Enable(string projectDir)
    {
        var dir = Path.Combine(projectDir, ".hoblio");
        Directory.CreateDirectory(dir);
        _writer = new StreamWriter(Path.Combine(dir, "run.jsonl"), append: true) { AutoFlush = true };
    }

    public static void Write(RunEvent evt)
    {
        _writer?.WriteLine(RunEventJson.Serialize(evt));
    }
}
