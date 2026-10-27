using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed class FlowConnectionViewModel : INotifyPropertyChanged
{
    FlowNodeViewModel _source = null!;
    FlowNodeViewModel _target = null!;

    public FlowNodeViewModel Source
    {
        get => _source;
        init
        {
            _source = value;
            _source.PropertyChanged += OnNodeChanged;
        }
    }

    public FlowNodeViewModel Target
    {
        get => _target;
        init
        {
            _target = value;
            _target.PropertyChanged += OnNodeChanged;
        }
    }

    public EdgePort Port { get; set; } = EdgePort.Main;

    public Point SourcePoint => ConnectionAnchor(Source, fromOutput: true);
    public Point TargetPoint => ConnectionAnchor(Target, fromOutput: false);

    static Point ConnectionAnchor(FlowNodeViewModel node, bool fromOutput)
    {
        const double nodeWidth = 140;
        const double anchorY = 28;
        var x = fromOutput ? node.Location.X + nodeWidth : node.Location.X;
        return new Point(x, node.Location.Y + anchorY);
    }

    void OnNodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FlowNodeViewModel.Location))
        {
            OnPropertyChanged(nameof(SourcePoint));
            OnPropertyChanged(nameof(TargetPoint));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
