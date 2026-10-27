using System.Text.Json;
using Hoblio.Model;

namespace Hoblio.Runtime;

public sealed class RunVariables : Dictionary<string, string>
{
    public static RunVariables SeedFromProject(Dictionary<string, JsonElement> values)
    {
        var vars = new RunVariables();
        foreach (var (key, value) in values)
        {
            if (value.ValueKind == JsonValueKind.String)
                vars[key] = value.GetString() ?? "";
            else if (value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                vars[key] = value.GetRawText();
            else if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                vars[key] = value.ToString();
        }
        return vars;
    }

    public static string Interpolate(string template, RunVariables vars)
    {
        if (!template.Contains('{'))
            return template;

        var outText = new System.Text.StringBuilder();
        var rest = template;
        while (true)
        {
            var start = rest.IndexOf('{');
            if (start < 0)
            {
                outText.Append(rest);
                break;
            }
            outText.Append(rest.AsSpan(0, start));
            rest = rest[(start + 1)..];
            var end = rest.IndexOf('}');
            if (end < 0)
                throw new InvalidOperationException("unclosed `{` in text — use `{variable_name}`");
            var name = rest[..end];
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("empty variable name in `{}`");
            if (!name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
                throw new InvalidOperationException($"invalid variable name `{name}` in template");
            if (!vars.TryGetValue(name, out var value))
                throw new InvalidOperationException($"variable `{name}` is not set (assign with browser_get_text or flow variables)");
            outText.Append(value);
            rest = rest[(end + 1)..];
        }
        return outText.ToString();
    }
}
