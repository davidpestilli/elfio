namespace Hoblio.TestSupport;

public static class ExamplePaths
{
    /// <summary>Resolves demo project folder by name (flow-control-demo, etc.).</summary>
    public static string Resolve(string demoName)
    {
        var env = Environment.GetEnvironmentVariable("HOBLIO_EXAMPLES_ROOT");
        if (!string.IsNullOrWhiteSpace(env))
        {
            var fromEnv = Path.Combine(env, demoName);
            if (Directory.Exists(fromEnv))
                return fromEnv;
        }

        foreach (var root in CandidateRoots())
        {
            var path = Path.Combine(root, demoName);
            if (Directory.Exists(path))
                return path;
        }

        throw new DirectoryNotFoundException(
            $"Example `{demoName}` not found. Set HOBLIO_EXAMPLES_ROOT or clone examples under hoblio-c#/examples.");
    }

    static IEnumerable<string> CandidateRoots()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            yield return Path.Combine(dir, "examples");
            yield return Path.Combine(dir, "..", "hoblio", "examples");
            dir = Path.GetFullPath(Path.Combine(dir, ".."));
        }
    }
}
