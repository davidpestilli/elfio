using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hoblio.Studio;

public enum ScalarVariableKind { Text, Number, Boolean }

public sealed class ScalarVariableRow
{
    public required string Name { get; set; }
    public ScalarVariableKind Kind { get; set; }
    public string TextValue { get; set; } = "";
    public double NumberValue { get; set; }
    public bool BoolValue { get; set; }
}

public sealed class ListVariableRow
{
    public required string Name { get; set; }
    public IList<string> Items { get; } = [];
}

public static class ProjectVariablesEditor
{
    static readonly Regex NameRe = new("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.CultureInvariant);

    public static bool IsValidVariableName(string name) =>
        !string.IsNullOrEmpty(name) && NameRe.IsMatch(name);

    public static (List<ScalarVariableRow> Scalars, List<ListVariableRow> Lists, List<string> OtherKeys)
        Partition(Dictionary<string, JsonElement> variables)
    {
        var scalars = new List<ScalarVariableRow>();
        var lists = new List<ListVariableRow>();
        var other = new List<string>();
        foreach (var (name, el) in variables)
        {
            if (el.ValueKind == JsonValueKind.Array)
            {
                var row = new ListVariableRow { Name = name };
                foreach (var item in el.EnumerateArray())
                    row.Items.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : item.ToString());
                lists.Add(row);
            }
            else if (el.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null)
            {
                scalars.Add(ScalarFromElement(name, el));
            }
            else
                other.Add(name);
        }
        return (scalars, lists, other);
    }

    static ScalarVariableRow ScalarFromElement(string name, JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.True => new ScalarVariableRow { Name = name, Kind = ScalarVariableKind.Boolean, BoolValue = true },
        JsonValueKind.False => new ScalarVariableRow { Name = name, Kind = ScalarVariableKind.Boolean, BoolValue = false },
        JsonValueKind.Number => new ScalarVariableRow { Name = name, Kind = ScalarVariableKind.Number, NumberValue = el.GetDouble() },
        _ => new ScalarVariableRow { Name = name, Kind = ScalarVariableKind.Text, TextValue = el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString() },
    };

    public static Dictionary<string, JsonElement> Build(
        IEnumerable<ScalarVariableRow> scalars,
        IEnumerable<ListVariableRow> lists,
        Dictionary<string, JsonElement> baseVars)
    {
        var (_, _, otherKeys) = Partition(baseVars);
        var outDict = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var key in otherKeys)
            outDict[key] = baseVars[key].Clone();

        foreach (var s in scalars)
        {
            var name = s.Name.Trim();
            if (name.Length == 0)
                continue;
            outDict[name] = s.Kind switch
            {
                ScalarVariableKind.Boolean => JsonSerializer.SerializeToElement(s.BoolValue),
                ScalarVariableKind.Number => JsonSerializer.SerializeToElement(s.NumberValue),
                _ => JsonSerializer.SerializeToElement(s.TextValue),
            };
        }

        foreach (var list in lists)
        {
            var name = list.Name.Trim();
            if (name.Length == 0)
                continue;
            outDict[name] = JsonSerializer.SerializeToElement(list.Items.ToList());
        }

        return outDict;
    }

    public static string ToPrettyJson(Dictionary<string, JsonElement> variables) =>
        JsonSerializer.Serialize(variables, new JsonSerializerOptions { WriteIndented = true });
}
