using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Hoblio.Model;
using Hoblio.Studio;

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

    public List<Point> Waypoints { get; } = [];

    bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowWaypointTools));
        }
    }

    public bool ShowWaypointTools => _isSelected;

    public void BindAnchorUpdates()
    {
        if (Source.Output is { } output)
            output.PropertyChanged += OnConnectorChanged;
        if (Target.Input is { } input)
            input.PropertyChanged += OnConnectorChanged;
    }

    public Geometry RouteGeometry
    {
        get
        {
            var anchors = new List<Point> { SourcePoint };
            anchors.AddRange(Waypoints);
            anchors.Add(TargetPoint);
            return FlowSmoothStepPath.Build(anchors);
        }
    }

    /// <summary>Source, manual waypoints, target — same anchor list as React Flow <c>WaypointEdge</c>.</summary>
    public IReadOnlyList<Point> HandlePoints
    {
        get
        {
            var list = new List<Point> { SourcePoint };
            list.AddRange(Waypoints);
            list.Add(TargetPoint);
            return list;
        }
    }

    public bool HasManualWaypoints => Waypoints.Count > 0;

    /// <summary>Polyline used for arrows and segment midpoints when waypoints exist; otherwise smooth-step samples.</summary>
    public IReadOnlyList<Point> GetPolylinePoints()
    {
        if (HasManualWaypoints)
            return HandlePoints.ToList();

        var geom = RouteGeometry;
        if (geom is PathGeometry pg && pg.Figures.Count > 0)
            return FlattenFigure(pg.Figures[0]).ToList();
        return HandlePoints.ToList();
    }

    static IEnumerable<Point> FlattenFigure(PathFigure figure)
    {
        yield return figure.StartPoint;
        var current = figure.StartPoint;
        foreach (var seg in figure.Segments)
        {
            switch (seg)
            {
                case LineSegment line:
                    current = line.Point;
                    yield return current;
                    break;
                case QuadraticBezierSegment quad:
                    current = quad.Point2;
                    yield return current;
                    break;
                case PolyLineSegment poly:
                    foreach (var p in poly.Points)
                    {
                        current = p;
                        yield return current;
                    }
                    break;
            }
        }
    }

    public PointCollection PolylinePoints
    {
        get
        {
            var pts = new PointCollection();
            foreach (var p in GetPolylinePoints())
                pts.Add(p);
            return pts;
        }
    }

    public Point SourcePoint => ResolveAnchor(Source.Output, Source, fromOutput: true);
    public Point TargetPoint => ResolveAnchor(Target.Input, Target, fromOutput: false);

    static Point ResolveAnchor(FlowConnectorViewModel? connector, FlowNodeViewModel node, bool fromOutput) =>
        connector is not null ? connector.Anchor : EstimateAnchor(node, fromOutput);

    static Point EstimateAnchor(FlowNodeViewModel node, bool fromOutput)
    {
        const double cardWidth = 150;
        const double portOffset = 18;
        const double anchorY = 28;
        var x = fromOutput
            ? node.Location.X + portOffset + cardWidth + portOffset
            : node.Location.X + portOffset;
        return new Point(x, node.Location.Y + anchorY);
    }

    void OnNodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FlowNodeViewModel.Location))
            NotifyGeometryChanged();
    }

    void OnConnectorChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FlowConnectorViewModel.Anchor))
            NotifyGeometryChanged();
    }

    public event EventHandler? GeometryChanged;

    public PointCollection ArrowHeadPoints
    {
        get
        {
            var pts = GetPolylinePoints();
            if (pts.Count < 2)
                return [];
            var tip = pts[^1];
            var prev = pts[^2];
            var dx = tip.X - prev.X;
            var dy = tip.Y - prev.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01)
                return [];
            dx /= len;
            dy /= len;
            const double size = 9;
            const double spread = 5;
            var basePt = new Point(tip.X - dx * size, tip.Y - dy * size);
            var orthoX = -dy * spread;
            var orthoY = dx * spread;
            return new PointCollection
            {
                tip,
                new(basePt.X + orthoX, basePt.Y + orthoY),
                new(basePt.X - orthoX, basePt.Y - orthoY),
            };
        }
    }

    public void NotifyGeometryChanged()
    {
        OnPropertyChanged(nameof(SourcePoint));
        OnPropertyChanged(nameof(TargetPoint));
        OnPropertyChanged(nameof(RouteGeometry));
        OnPropertyChanged(nameof(ArrowHeadPoints));
        OnPropertyChanged(nameof(PolylinePoints));
        GeometryChanged?.Invoke(this, EventArgs.Empty);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
