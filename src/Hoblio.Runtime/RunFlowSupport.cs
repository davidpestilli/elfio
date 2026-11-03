using System.Text.Json;
using Hoblio.Model;

namespace Hoblio.Runtime;

static class RunFlowSupport
{
    public static void ApplyInheritInputs(
        Dictionary<string, string>? inputs,
        Dictionary<string, JsonElement> childSeeds,
        RunVariables runVars)
    {
        foreach (var (key, value) in RunVariables.SeedFromProject(childSeeds))
        {
            if (!runVars.ContainsKey(key))
                runVars[key] = value;
        }
        if (inputs is null)
            return;
        foreach (var (varName, template) in inputs)
            runVars[varName] = RunVariables.Interpolate(template, runVars);
    }

    public static void ApplyIsolatedInputs(
        Dictionary<string, string>? inputs,
        Dictionary<string, JsonElement> childSeeds,
        RunVariables parentVars,
        RunVariables childVars)
    {
        childVars.Clear();
        foreach (var (key, value) in RunVariables.SeedFromProject(childSeeds))
            childVars[key] = value;
        if (inputs is null)
            return;
        foreach (var (varName, template) in inputs)
            childVars[varName] = RunVariables.Interpolate(template, parentVars);
    }

    public static void MergeIsolatedOutputs(
        RunVariables parentVars,
        RunVariables parentSnapshot,
        RunVariables childVars,
        List<string>? outputs)
    {
        foreach (var kv in parentSnapshot)
            parentVars[kv.Key] = kv.Value;
        if (outputs is not { Count: > 0 })
            return;
        foreach (var name in outputs)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;
            if (childVars.TryGetValue(name, out var value))
                parentVars[name] = value;
        }
    }

    public static string Summary(string flowRef, string endNode, List<string>? outputs, RunVariables runVars)
    {
        var summary = $"run_flow `{flowRef}` finished at `{endNode}`";
        if (outputs is not { Count: > 0 })
            return summary;
        var parts = new List<string>();
        foreach (var name in outputs)
        {
            if (runVars.TryGetValue(name, out var value))
            {
                var preview = value.Length <= 40 ? value : value[..40];
                parts.Add($"{name}={preview}");
            }
        }
        if (parts.Count > 0)
            summary += " (" + string.Join(", ", parts) + ")";
        return summary;
    }
}
