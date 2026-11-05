using System.Globalization;
using System.Text.Json;
using Hoblio.Model;

namespace Hoblio.Runtime;

public static class ListActions
{
    public static string ListClear(RunVariables vars, string listVariable)
    {
        var name = listVariable.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("list_clear: variable name is empty");
        vars[name] = "[]";
        return $"list_clear: `{name}`";
    }

    public static string ListAppend(RunVariables vars, string listVariable, string itemTemplate)
    {
        var name = listVariable.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("list_append: variable name is empty");
        var item = RunVariables.Interpolate(itemTemplate, vars);
        var items = ReadListVar(vars, name);
        items.Add(item);
        WriteListVar(vars, name, items);
        return $"list_append: `{name}` (+1 item, len={items.Count})";
    }

    public static string RemoveItem(VariablesAction action, RunVariables vars)
    {
        var name = RequireListVar(action.ListVariable);
        var items = ReadListVar(vars, name);
        var mode = (action.Mode ?? VariablesEnums.RemoveListItemMode.ByIndex).Trim().ToLowerInvariant();
        if (mode == VariablesEnums.RemoveListItemMode.ByValue)
        {
            var value = RunVariables.Interpolate(action.Value ?? "", vars);
            var ignoreCase = action.IgnoreCase ?? false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (action.RemoveAllMatches == true)
                items.RemoveAll(i => string.Equals(i, value, comparison));
            else
            {
                var idx = items.FindIndex(i => string.Equals(i, value, comparison));
                if (idx < 0)
                    throw new InvalidOperationException($"list item `{value}` not found");
                items.RemoveAt(idx);
            }
        }
        else
        {
            var idxText = RunVariables.Interpolate(action.ItemIndex ?? action.Value ?? "", vars);
            if (!int.TryParse(idxText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx)
                || idx < 0 || idx >= items.Count)
                throw new InvalidOperationException($"list index `{idxText}` out of range");
            items.RemoveAt(idx);
        }
        WriteListVar(vars, name, items);
        return $"{action.Operation}: `{name}` len={items.Count}";
    }

    public static string SortList(VariablesAction action, RunVariables vars)
    {
        var name = RequireListVar(action.ListVariable);
        var items = ReadListVar(vars, name);
        var desc = VariablesSemantics.SortDescending(action.SortOrder);
        var ignoreCase = !(action.MatchCase ?? false);
        var prop = action.PropertyName?.Trim();
        items.Sort((a, b) =>
        {
            var av = string.IsNullOrEmpty(prop) ? a : ReadJsonProperty(a, prop);
            var bv = string.IsNullOrEmpty(prop) ? b : ReadJsonProperty(b, prop);
            var cmp = CompareSortValues(av, bv, ignoreCase);
            return desc ? -cmp : cmp;
        });
        WriteListVar(vars, name, items);
        return $"{action.Operation}: `{name}`";
    }

    static int CompareSortValues(string a, string b, bool ignoreCase)
    {
        if (double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out var an)
            && double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out var bn))
            return an.CompareTo(bn);
        return string.Compare(a, b, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    internal static string ReadJsonPropertyPublic(string jsonItem, string prop) => ReadJsonProperty(jsonItem, prop);

    static string ReadJsonProperty(string jsonItem, string prop)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonItem);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty(prop, out var el))
                return el.ToString();
        }
        catch (JsonException)
        {
            // plain string item
        }
        return jsonItem;
    }

    public static string ShuffleList(VariablesAction action, RunVariables vars)
    {
        var name = RequireListVar(action.ListVariable);
        var items = ReadListVar(vars, name);
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
        WriteListVar(vars, name, items);
        return $"{action.Operation}: `{name}`";
    }

    public static string MergeLists(VariablesAction action, RunVariables vars)
    {
        var a = RequireListVar(action.ListVariable);
        var b = RequireListVar(action.SecondListVariable);
        var merged = ReadListVar(vars, a);
        merged.AddRange(ReadListVar(vars, b));
        var outName = action.OutputVariable?.Trim();
        if (string.IsNullOrEmpty(outName))
            outName = a;
        WriteListVar(vars, outName, merged);
        return $"{action.Operation}: `{outName}` len={merged.Count}";
    }

    public static string ReverseList(VariablesAction action, RunVariables vars)
    {
        var name = RequireListVar(action.ListVariable);
        var items = ReadListVar(vars, name);
        items.Reverse();
        WriteListVar(vars, name, items);
        return $"{action.Operation}: `{name}`";
    }

    public static string RemoveDuplicates(VariablesAction action, RunVariables vars)
    {
        var name = RequireListVar(action.ListVariable);
        var items = ReadListVar(vars, name);
        var ignoreCase = action.IgnoreCase ?? false;
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var seen = new HashSet<string>(comparer);
        var unique = new List<string>();
        foreach (var item in items)
        {
            if (seen.Add(item))
                unique.Add(item);
        }
        WriteListVar(vars, name, unique);
        return $"{action.Operation}: `{name}` len={unique.Count}";
    }

    public static string FindCommon(VariablesAction action, RunVariables vars)
    {
        var a = RequireListVar(action.ListVariable);
        var b = RequireListVar(action.SecondListVariable);
        var ignoreCase = action.IgnoreCase ?? false;
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var setB = new HashSet<string>(ReadListVar(vars, b), comparer);
        var common = ReadListVar(vars, a).Where(setB.Contains).Distinct(comparer).ToList();
        var outName = RequireListVar(action.OutputVariable ?? action.ListVariable);
        WriteListVar(vars, outName, common);
        return $"{action.Operation}: `{outName}` len={common.Count}";
    }

    public static string SubtractLists(VariablesAction action, RunVariables vars)
    {
        var a = RequireListVar(action.ListVariable);
        var b = RequireListVar(action.SecondListVariable);
        var ignoreCase = action.IgnoreCase ?? false;
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var remove = new HashSet<string>(ReadListVar(vars, b), comparer);
        var result = ReadListVar(vars, a).Where(i => !remove.Contains(i)).ToList();
        var outName = action.OutputVariable?.Trim();
        if (string.IsNullOrEmpty(outName))
            outName = a;
        WriteListVar(vars, outName, result);
        return $"{action.Operation}: `{outName}` len={result.Count}";
    }

    public static string FilterList(VariablesAction action, RunVariables vars)
    {
        var name = RequireListVar(action.ListVariable);
        var items = ReadListVar(vars, name);
        var filtered = items.Where(item => VariablesFilterConditions.ListItemMatches(item, action, vars)).ToList();
        var outName = action.OutputVariable?.Trim();
        if (string.IsNullOrEmpty(outName))
            outName = name;
        WriteListVar(vars, outName, filtered);
        return $"{action.Operation}: `{outName}` len={filtered.Count}";
    }

    static string RequireListVar(string? name)
    {
        var trimmed = name?.Trim() ?? "";
        if (string.IsNullOrEmpty(trimmed))
            throw new InvalidOperationException("list_variable is required");
        return trimmed;
    }

    static List<string> ReadListVar(RunVariables vars, string name)
    {
        if (!vars.TryGetValue(name, out var raw))
            return [];
        return ExpressionEvaluator.ParseStringList(raw);
    }

    static void WriteListVar(RunVariables vars, string name, List<string> items)
    {
        vars[name] = JsonSerializer.Serialize(items);
    }
}
