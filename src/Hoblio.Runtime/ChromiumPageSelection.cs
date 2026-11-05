using Hoblio.Model;

namespace Hoblio.Runtime;

/// <summary>Escolha de aba alinhada a AttachToChrome/Edge ByUrl, ByTitle, Foreground (PAD).</summary>
public static class ChromiumPageSelection
{
    public sealed record Candidate(string Url, string Title);

    public static Candidate? Pick(
        IReadOnlyList<Candidate> pages,
        string attachMode,
        string? urlFragment,
        string? titleFragment,
        string? foregroundWindowTitle)
    {
        var usable = pages
            .Where(p => !IsInternalUrl(p.Url))
            .ToList();
        if (usable.Count == 0)
            return null;

        if (string.Equals(attachMode, WebAutomationEnums.AttachMode.Foreground, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(foregroundWindowTitle))
                return usable[0];
            var normalized = Win32Foreground.NormalizeBrowserWindowTitle(foregroundWindowTitle);
            return FindBestTitleMatch(usable, normalized) ?? usable[0];
        }

        if (string.Equals(attachMode, WebAutomationEnums.AttachMode.ByUrl, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(urlFragment))
                throw new InvalidOperationException("browser attach by_url requires `url`");
            return FindContains(usable, urlFragment, (c, f) => c.Url.Contains(f, StringComparison.OrdinalIgnoreCase));
        }

        if (string.Equals(attachMode, WebAutomationEnums.AttachMode.ByTitle, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(titleFragment))
                throw new InvalidOperationException("browser attach by_title requires `text` (título)");
            return FindContains(usable, titleFragment, (c, f) => c.Title.Contains(f, StringComparison.OrdinalIgnoreCase));
        }

        throw new InvalidOperationException($"unknown attach_mode `{attachMode}`");
    }

    static Candidate? FindContains(
        IReadOnlyList<Candidate> pages,
        string fragment,
        Func<Candidate, string, bool> predicate)
    {
        Candidate? best = null;
        var bestLen = int.MaxValue;
        foreach (var p in pages)
        {
            if (!predicate(p, fragment))
                continue;
            var len = p.Url.Length + p.Title.Length;
            if (len < bestLen)
            {
                best = p;
                bestLen = len;
            }
        }
        return best;
    }

    static Candidate? FindBestTitleMatch(IReadOnlyList<Candidate> pages, string normalizedWindowTitle)
    {
        Candidate? exact = null;
        Candidate? contains = null;
        foreach (var p in pages)
        {
            if (p.Title.Equals(normalizedWindowTitle, StringComparison.OrdinalIgnoreCase))
                exact = p;
            else if (normalizedWindowTitle.Contains(p.Title, StringComparison.OrdinalIgnoreCase)
                     || p.Title.Contains(normalizedWindowTitle, StringComparison.OrdinalIgnoreCase))
                contains ??= p;
        }
        return exact ?? contains;
    }

    static bool IsInternalUrl(string url) =>
        url.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("devtools://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("edge://", StringComparison.OrdinalIgnoreCase);
}
