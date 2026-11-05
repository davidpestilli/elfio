using Hoblio.Model;

namespace Hoblio.Runtime;

public static class ChromiumLocator
{
    const string ChromeRel = @"Google\Chrome\Application\chrome.exe";
    const string EdgeRel = @"Microsoft\Edge\Application\msedge.exe";

    public static string ResolveExecutable() => ResolveExecutable(null);

    public static string ResolveExecutable(string? browserEngine)
    {
        var overridePath = Environment.GetEnvironmentVariable("HOBLIO_CHROME_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            if (File.Exists(overridePath))
                return overridePath;
            throw new InvalidOperationException($"HOBLIO_CHROME_PATH is set but not a file: {overridePath}");
        }

        var preferEdge = string.Equals(browserEngine, WebAutomationEnums.BrowserEngine.Edge, StringComparison.OrdinalIgnoreCase);
        var preferChrome = string.Equals(browserEngine, WebAutomationEnums.BrowserEngine.Chrome, StringComparison.OrdinalIgnoreCase);

        foreach (var candidate in Candidates(preferEdge, preferChrome))
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new InvalidOperationException(
            "Chromium browser not found. Install Google Chrome or Microsoft Edge, or set HOBLIO_CHROME_PATH to chrome.exe / msedge.exe");
    }

    static IEnumerable<string> Candidates(bool preferEdge, bool preferChrome)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> RelOrder()
        {
            if (preferEdge) { yield return EdgeRel; yield return ChromeRel; yield break; }
            if (preferChrome) { yield return ChromeRel; yield return EdgeRel; yield break; }
            yield return ChromeRel;
            yield return EdgeRel;
        }

        foreach (var rel in RelOrder())
        {
            foreach (var path in BuildList(Environment.GetEnvironmentVariable("ProgramFiles"), rel))
                if (seen.Add(path)) yield return path;
            foreach (var path in BuildList(Environment.GetEnvironmentVariable("ProgramFiles(x86)"), rel))
                if (seen.Add(path)) yield return path;
            var local = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(local))
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
