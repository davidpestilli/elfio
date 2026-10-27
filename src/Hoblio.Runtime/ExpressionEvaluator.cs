using System.Globalization;
using System.Text.Json;
using Hoblio.Model;

namespace Hoblio.Runtime;

public static class ExpressionEvaluator
{
    public static string ResolveOperand(string raw, RunVariables vars)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}') && trimmed.Length >= 2)
        {
            var name = trimmed[1..^1];
            if (!vars.TryGetValue(name, out var value))
                throw new InvalidOperationException($"variable `{name}` is not set");
            return value;
        }
        if (vars.TryGetValue(trimmed, out var direct))
            return direct;
        return trimmed;
    }

    public static double ParseNumber(string raw)
    {
        if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
            throw new InvalidOperationException($"expected a number, got `{raw}`");
        return n;
    }

    public static bool EvalIf(IfControl cfg, RunVariables vars)
    {
        var left = ResolveOperand(cfg.Left, vars);
        var right = ResolveOperand(cfg.Right, vars);

        if (IsNumericOperator(cfg.Operator))
        {
            var l = ParseNumber(left);
            var r = ParseNumber(right);
            return cfg.Operator switch
            {
                CompareOperator.Gt => l > r,
                CompareOperator.Gte => l >= r,
                CompareOperator.Lt => l < r,
                CompareOperator.Lte => l <= r,
                _ => throw new InvalidOperationException("unexpected numeric operator"),
            };
        }

        if (cfg.IgnoreCase)
        {
            left = left.ToLowerInvariant();
            right = right.ToLowerInvariant();
        }

        return cfg.Operator switch
        {
            CompareOperator.Eq => left == right,
            CompareOperator.Ne => left != right,
            CompareOperator.Contains => left.Contains(right, StringComparison.Ordinal),
            _ => throw new InvalidOperationException("unexpected operator"),
        };
    }

    public static List<string> ParseStringList(string raw)
    {
        var trimmed = raw.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return [];
        using var doc = JsonDocument.Parse(trimmed);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("list variable must be a JSON array");
        var outList = new List<string>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            outList.Add(item.ValueKind switch
            {
                JsonValueKind.String => item.GetString() ?? "",
                JsonValueKind.Number => item.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "",
                _ => item.GetRawText(),
            });
        }
        return outList;
    }

    public static List<string> ListFromVars(string name, RunVariables vars)
    {
        if (!vars.TryGetValue(name, out var raw))
            throw new InvalidOperationException($"list variable `{name}` is not set");
        try
        {
            return ParseStringList(raw);
        }
        catch
        {
            return [raw];
        }
    }

    static bool IsNumericOperator(CompareOperator op) =>
        op is CompareOperator.Gt or CompareOperator.Gte or CompareOperator.Lt or CompareOperator.Lte;
}
