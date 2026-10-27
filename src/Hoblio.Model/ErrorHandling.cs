namespace Hoblio.Model;

/// <summary>Per action-node retry and failure policy (PAD-style subset).</summary>
public sealed class ErrorHandling
{
    public uint RetryCount { get; set; }

    public ulong? RetryDelayMs { get; set; }

    public OnFailure OnFailure { get; set; } = OnFailure.Stop;

    public uint Attempts() => 1 + RetryCount;

    public bool ContinuesOnFailure() => OnFailure == OnFailure.Continue;
}
