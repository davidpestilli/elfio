namespace Hoblio.Runtime;

/// <summary>Run failures with optional context chain (anyhow-style).</summary>
public sealed class HoblioRunException : Exception
{
    public HoblioRunException(string message) : base(message) { }

    public HoblioRunException(string message, Exception inner) : base(message, inner) { }
}

public static class RunErrors
{
    public static string FormatChain(Exception ex)
    {
        var lines = new List<string> { ex.Message };
        var inner = ex.InnerException;
        var i = 0;
        while (inner is not null)
        {
            lines.Add($"    {i}: {inner.Message}");
            inner = inner.InnerException;
            i++;
        }
        if (i == 0)
            return ex.Message;
        return string.Join(Environment.NewLine, lines[0], "Caused by:", string.Join(Environment.NewLine, lines.Skip(1)));
    }

    public static string CliMessage(Exception ex)
    {
        if (ex is HoblioRunException)
            return FormatChain(ex);
        return ex.Message;
    }

    /// <summary>Single-line friendly summary for on_failure continue logs.</summary>
    public static string ContinueSummary(Exception ex) =>
        ex is HoblioRunException ? FormatChain(ex) : ex.Message;
}
