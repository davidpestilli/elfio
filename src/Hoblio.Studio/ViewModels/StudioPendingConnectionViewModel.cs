using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Hoblio.Studio.ViewModels;

public sealed class StudioPendingConnectionViewModel : INotifyPropertyChanged
{
    FlowConnectorViewModel _source = null!;
    public FlowConnectorViewModel Source
    {
        get => _source;
        set { _source = value; OnPropertyChanged(); }
    }

    FlowConnectorViewModel? _target;
    public FlowConnectorViewModel? Target
    {
        get => _target;
        set { _target = value; OnPropertyChanged(); }
    }

    bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; OnPropertyChanged(); }
    }

    Point _targetLocation;
    public Point TargetLocation
    {
        get => _targetLocation;
        set { _targetLocation = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
