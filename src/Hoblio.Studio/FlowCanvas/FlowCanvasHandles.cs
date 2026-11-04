using Hoblio.Model;

namespace Hoblio.Studio.FlowCanvas;

/// <summary>Maps React Flow handle ids ↔ <see cref="EdgePort"/> (mirrors Rust <c>flowConvert.ts</c>).</summary>
public static class FlowCanvasHandles
{
    public const string MainSource = "right";
    static readonly HashSet<string> MainOut = new(StringComparer.Ordinal)
    {
        "right", "top", "bottom", "left", "out-top", "out-bottom",
    };

    public static EdgePort PortFromSourceHandle(NodeKind sourceKind, string? handle)
    {
        if (EdgePortExtensions.Parse(handle) is { } port)
            return port;
        if (string.IsNullOrEmpty(handle) || MainOut.Contains(handle))
            return EdgePort.Main;
        return EdgePort.Main;
    }

    public static string SourceHandleFor(NodeKind kind, EdgePort port, string? persisted)
    {
        if (!string.IsNullOrEmpty(persisted))
            return NormalizeSourceHandle(persisted)!;
        if (port is not EdgePort.Main)
            return port.AsString();
        return kind is NodeKind.If or NodeKind.ForEach or NodeKind.WhileLoop or NodeKind.Repeat
            ? port.AsString()
            : MainSource;
    }

    public static string? NormalizeSourceHandle(string? value) => value switch
    {
        "out-top" => "top",
        "out-bottom" => "bottom",
        _ => value,
    };

    public static string DefaultTargetHandle(string? persisted) =>
        string.IsNullOrEmpty(persisted) ? "left" : persisted;

    public static string? EdgeLabelForPort(EdgePort port) =>
        port == EdgePort.Main ? null : port.AsString();

    public static string? CanvasEdgeLabel(EdgePort port, string? customLabel)
    {
        if (!string.IsNullOrWhiteSpace(customLabel))
            return customLabel.Trim();
        return EdgeLabelForPort(port);
    }
}
