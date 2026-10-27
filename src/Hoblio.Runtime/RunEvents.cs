namespace Hoblio.Runtime;

public abstract record RunEvent
{
    public sealed record RunStarted(string Project) : RunEvent;
    public sealed record NodeEnter(string NodeId) : RunEvent;
    public sealed record NodeExit(string NodeId) : RunEvent;
    public sealed record ActionExecuted(string NodeId, string Summary, Dictionary<string, string>? Variables) : RunEvent;
    public sealed record RunFinished(string LastNode) : RunEvent;
}

public sealed class RunOutcome
{
    public required List<RunEvent> Events { get; init; }
    public required string LastNodeId { get; init; }
}

public static class RunCancel
{
    static int _requested;

    public static void Reset() => Interlocked.Exchange(ref _requested, 0);

    public static void Request() => Interlocked.Exchange(ref _requested, 1);

    public static bool IsRequested => Interlocked.CompareExchange(ref _requested, 0, 0) != 0;
}
