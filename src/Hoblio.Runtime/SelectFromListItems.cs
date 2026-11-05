namespace Hoblio.Runtime;

static class SelectFromListItems
{
    public static List<string> Parse(string raw, RunVariables vars)
    {
        var interpolated = RunVariables.Interpolate(raw, vars).Trim();
        if (string.IsNullOrEmpty(interpolated))
            return [];

        if (interpolated.StartsWith('['))
        {
            try
            {
                return ExpressionEvaluator.ParseStringList(interpolated);
            }
            catch
            {
                // fall through to line split
            }
        }

        return interpolated
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
