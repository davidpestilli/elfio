using System.Text.Json;
using Hoblio.Model;

namespace Hoblio.Runtime;

static class VariablesFilterConditions
{
    internal readonly record struct Condition(string Column, string? Operator, string? Value, string? Logic);

    internal static bool RowMatchesExtended(FlowTable table, List<string> row, VariablesAction action, RunVariables vars)
    {
        var parsed = Parse(action.FilterConditions);
        if (parsed.Count > 0)
            return EvaluateChain(table, row, parsed, action, vars);

        var logic = (action.FilterLogic ?? VariablesEnums.FilterLogic.And).Trim().ToLowerInvariant();
        var m1 = FlowTable.MatchFilter(table, row, action.FilterColumn, action.FilterOperator, action.FilterValue, vars, action.IgnoreCase ?? false);
        if (string.IsNullOrWhiteSpace(action.FilterColumn2))
            return m1;
        var m2 = FlowTable.MatchFilter(table, row, action.FilterColumn2, action.FilterOperator2, action.FilterValue2, vars, action.IgnoreCase ?? false);
        return logic == VariablesEnums.FilterLogic.Or ? m1 || m2 : m1 && m2;
    }

    internal static bool ListItemMatches(string item, VariablesAction action, RunVariables vars)
    {
        var parsed = Parse(action.FilterConditions);
        var prop = action.PropertyName?.Trim();
        var haystack = string.IsNullOrEmpty(prop) ? item : ListActions.ReadJsonPropertyPublic(item, prop);
        var ignoreCase = action.IgnoreCase ?? true;

        if (parsed.Count > 0)
        {
            bool acc = MatchValue(haystack, parsed[0], vars, ignoreCase);
            for (var i = 1; i < parsed.Count; i++)
            {
                var m = MatchValue(haystack, parsed[i], vars, ignoreCase);
                var logic = (parsed[i].Logic ?? action.FilterLogic ?? VariablesEnums.FilterLogic.And).Trim().ToLowerInvariant();
                acc = logic == VariablesEnums.FilterLogic.Or ? acc || m : acc && m;
            }
            return acc;
        }

        var logic2 = (action.FilterLogic ?? VariablesEnums.FilterLogic.And).Trim().ToLowerInvariant();
        var needle = RunVariables.Interpolate(action.Value ?? action.FilterValue ?? "", vars);
        var m1 = VariablesSemantics.EvaluateFilter(haystack, action.FilterOperator, needle, ignoreCase);
        if (string.IsNullOrWhiteSpace(action.FilterColumn2))
            return m1;
        var needle2 = RunVariables.Interpolate(action.FilterValue2 ?? "", vars);
        var m2 = VariablesSemantics.EvaluateFilter(haystack, action.FilterOperator2, needle2, ignoreCase);
        return logic2 == VariablesEnums.FilterLogic.Or ? m1 || m2 : m1 && m2;
    }

    static bool EvaluateChain(FlowTable table, List<string> row, IReadOnlyList<Condition> parsed, VariablesAction action, RunVariables vars)
    {
        var ignoreCase = action.IgnoreCase ?? false;
        bool acc = FlowTable.MatchFilter(table, row, parsed[0].Column, parsed[0].Operator, parsed[0].Value, vars, ignoreCase);
        for (var i = 1; i < parsed.Count; i++)
        {
            var m = FlowTable.MatchFilter(table, row, parsed[i].Column, parsed[i].Operator, parsed[i].Value, vars, ignoreCase);
            var logic = (parsed[i].Logic ?? action.FilterLogic ?? VariablesEnums.FilterLogic.And).Trim().ToLowerInvariant();
            acc = logic == VariablesEnums.FilterLogic.Or ? acc || m : acc && m;
        }
        return acc;
    }

    static bool MatchValue(string haystack, Condition c, RunVariables vars, bool ignoreCase)
    {
        var needle = RunVariables.Interpolate(c.Value ?? "", vars);
        return VariablesSemantics.EvaluateFilter(haystack, c.Operator, needle, ignoreCase);
    }

    static List<Condition> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];
        raw = raw.Trim();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];
            var list = new List<Condition>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                var col = GetProp(el, "column") ?? GetProp(el, "filter_column");
                if (string.IsNullOrWhiteSpace(col))
                    continue;
                list.Add(new Condition(
                    col,
                    GetProp(el, "operator") ?? GetProp(el, "filter_operator"),
                    GetProp(el, "value") ?? GetProp(el, "filter_value"),
                    GetProp(el, "logic") ?? GetProp(el, "filter_logic")));
            }
            return list;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    static string? GetProp(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
}
