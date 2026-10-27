using System.Text.Json;

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
