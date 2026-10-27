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
        validateCmd.SetHandler((string dir) =>
        {
            try { ValidateProject(dir); }
            catch (Exception ex) { Fail(ex); }
        }, projectArg);

        root.AddCommand(runCmd);
        root.AddCommand(validateCmd);
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

    static void ValidateProject(string projectDir)
    {
        ProjectLoader.ValidateProjectDir(projectDir);
        Console.Error.WriteLine($"valid: {projectDir}");
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
