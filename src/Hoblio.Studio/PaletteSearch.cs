namespace Hoblio.Studio;

public static class PaletteSearch
{
    public static string BuildSearchIndex(string key, string title, string? hint, string? aliases)
    {
        var parts = new List<string> { key, title };
        if (!string.IsNullOrWhiteSpace(hint))
            parts.Add(hint);
        if (!string.IsNullOrWhiteSpace(aliases))
            parts.Add(aliases);
        return string.Join('\n', parts);
    }

    public static bool Matches(string searchIndex, string query)
    {
        var q = query.Trim();
        if (q.Length == 0)
            return true;
        return searchIndex.Contains(q, StringComparison.OrdinalIgnoreCase);
    }
}
