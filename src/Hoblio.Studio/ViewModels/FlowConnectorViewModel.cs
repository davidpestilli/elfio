using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed class FlowConnectorViewModel : INotifyPropertyChanged
{
    FlowNodeViewModel _node = null!;

    public FlowNodeViewModel Node
    {
        get => _node;
        init
        {
            _node = value;
            _node.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(FlowNodeViewModel.Location))
                    RefreshAnchor();
            };
            RefreshAnchor();
        }
    }

    public required bool IsInput { get; init; }

    public EdgePort Port { get; init; } = EdgePort.Main;

    bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected == value)
                return;
            _isConnected = value;
            OnPropertyChanged();
        }
    }

    Point _anchor;
    public Point Anchor
    {
        get => _anchor;
        set
        {
            if (_anchor == value)
                return;
            _anchor = value;
            OnPropertyChanged();
        }
    }

    public void RefreshAnchor() => Anchor = ComputeAnchor();

    Point ComputeAnchor()
    {
        const double cardWidth = 150;
        const double port = 14;
        const double gap = 6;
        const double anchorY = 28;
        var cardLeft = Node.Location.X + (Node.HasInput ? port + gap : 0);
        if (IsInput)
            return new Point(Node.Location.X + port / 2, Node.Location.Y + anchorY);

        var outputs = Node.OutputConnectors;
        var idx = outputs.IndexOf(this);
        if (idx < 0)
            idx = 0;
        var count = Math.Max(outputs.Count, 1);
        var yOffset = count <= 1 ? anchorY : 14 + idx * (28.0 / Math.Max(count - 1, 1));
        return new Point(cardLeft + cardWidth + gap + port / 2, Node.Location.Y + yOffset);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
