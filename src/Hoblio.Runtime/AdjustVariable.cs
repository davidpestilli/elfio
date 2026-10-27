using System.Globalization;
using Hoblio.Model;

namespace Hoblio.Runtime;

public static class AdjustVariable
{
    public static string ExecuteAdjust(ActionKind action, RunVariables vars, double sign)
    {
        return action switch
        {
            IncreaseVariableAction inc => $"increase_variable: `{inc.Variable}` -> {Adjust(vars, inc.Variable, inc.Amount, Math.Abs(sign))}",
            DecreaseVariableAction dec => $"decrease_variable: `{dec.Variable}` -> {Adjust(vars, dec.Variable, dec.Amount, -Math.Abs(sign))}",
            _ => throw new InvalidOperationException("not an adjust variable action"),
        };
    }

    static string Adjust(RunVariables vars, string variable, string? amount, double sign)
    {
        var name = variable.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("variable name is empty");
        var amountRaw = amount is null ? "1" : RunVariables.Interpolate(amount, vars);
        double delta;
        try
        {
            delta = ExpressionEvaluator.ParseNumber(amountRaw) * sign;
        }
        catch (Exception ex)
        {
            throw new HoblioRunException($"variable `{name}` must be numeric for increase/decrease", ex);
        }
        double baseVal;
        if (vars.TryGetValue(name, out var current))
        {
            try
            {
                baseVal = ExpressionEvaluator.ParseNumber(current);
            }
            catch (Exception ex)
            {
                throw new HoblioRunException($"variable `{name}` must be numeric for increase/decrease", ex);
            }
        }
        else
            baseVal = 0.0;
        var newVal = baseVal + delta;
        var stored = FormatNumber(newVal);
        vars[name] = stored;
        return stored;
    }

    static string FormatNumber(double n)
    {
        var rounded = Math.Round(n);
        if (Math.Abs(n - rounded) < 1e-9)
            return ((long)rounded).ToString(CultureInfo.InvariantCulture);
        return n.ToString(CultureInfo.InvariantCulture);
    }
}
