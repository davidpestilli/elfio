using System.Windows;
using System.Windows.Media;
using Hoblio.Runtime;

namespace Hoblio.Studio.ViewModels;

public sealed class RunLogLine
{
    public RunLogLineKind Kind { get; init; }
    public int Indent { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public bool IsError { get; init; }
    public bool IsMuted { get; init; }
    public bool IsHidden { get; init; }
    public required string Human { get; init; }
    public required string Json { get; init; }
    public string? NodeId { get; init; }

    public bool CanFocusNode => !string.IsNullOrWhiteSpace(NodeId);

    public Thickness IndentMargin => new(8 + Indent * 16, 1, 8, 1);

    public Brush AccentBrush => Kind switch
    {
        RunLogLineKind.Run => Frozen("#38BDF8"),
        RunLogLineKind.Flow => Frozen("#A78BFA"),
        RunLogLineKind.Action when IsError => Frozen("#F87171"),
        RunLogLineKind.Action => Frozen("#4ADE80"),
        RunLogLineKind.Node => Frozen("#64748B"),
        _ => Frozen("#94A3B8"),
    };

    public Brush TitleBrush => IsError
        ? Frozen("#FCA5A5")
        : IsMuted
            ? Frozen("#9CA3AF")
            : Frozen("#F3F4F6");

    static Brush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }

    public static RunLogLine FromEvent(RunEvent evt, RunLogFormatter formatter) =>
        formatter.Format(evt);
}
