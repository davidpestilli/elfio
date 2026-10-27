namespace Hoblio.Runtime;

public static class ChromiumLocator
{
    const string ChromeRel = @"Google\Chrome\Application\chrome.exe";
    const string EdgeRel = @"Microsoft\Edge\Application\msedge.exe";

    public static string ResolveExecutable()
    {
        var overridePath = Environment.GetEnvironmentVariable("HOBLIO_CHROME_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            if (File.Exists(overridePath))
                return overridePath;
            throw new InvalidOperationException($"HOBLIO_CHROME_PATH is set but not a file: {overridePath}");
        }

        foreach (var candidate in Candidates())
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new InvalidOperationException(
            "Chromium browser not found. Install Google Chrome or Microsoft Edge, or set HOBLIO_CHROME_PATH to chrome.exe / msedge.exe");
    }

    static IEnumerable<string> Candidates()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in BuildList(Environment.GetEnvironmentVariable("ProgramFiles"), ChromeRel))
            if (seen.Add(path)) yield return path;
        foreach (var path in BuildList(Environment.GetEnvironmentVariable("ProgramFiles"), EdgeRel))
            if (seen.Add(path)) yield return path;
        foreach (var path in BuildList(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), ChromeRel))
            if (seen.Add(path)) yield return path;
        foreach (var path in BuildList(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), EdgeRel))
            if (seen.Add(path)) yield return path;
        var local = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrEmpty(local))
        {
            foreach (var rel in new[] { ChromeRel, EdgeRel })
            {
                var path = Path.Combine(local, rel);
                if (seen.Add(path))
                    yield return path;
            }
        }
    }

    static IEnumerable<string> BuildList(string? root, string rel)
    {
        if (string.IsNullOrEmpty(root))
            yield break;
        yield return Path.Combine(root, rel);
    }
}
