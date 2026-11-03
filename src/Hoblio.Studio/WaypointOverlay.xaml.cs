using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Hoblio.Studio.ViewModels;
using Nodify;

namespace Hoblio.Studio;

public partial class WaypointOverlay
{
    FlowEditorViewModel? _vm;
    FlowConnectionViewModel? _connection;
    NodifyEditor? _editor;
    int _dragIndex = -1;
    bool _dragHistoryPushed;

    public WaypointOverlay()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => HookVm();
        Loaded += (_, _) => HookVm();
    }

    public void AttachEditor(NodifyEditor editor)
    {
        if (_editor is not null)
            _editor.ViewportUpdated -= OnViewportUpdated;
        _editor = editor;
        _editor.ViewportUpdated += OnViewportUpdated;
        SyncViewportTransform();
    }

    void OnViewportUpdated(object? sender, EventArgs e)
    {
        SyncViewportTransform();
        Redraw();
    }

    void SyncViewportTransform()
    {
        if (_editor is null)
            return;
        Root.RenderTransform = _editor.ViewportTransform;
    }

    void HookVm()
    {
        if (DataContext is not FlowEditorViewModel vm)
            return;
        if (ReferenceEquals(_vm, vm))
            return;
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = vm;
        vm.PropertyChanged += OnVmPropertyChanged;
        if (_connection is not null)
            _connection.GeometryChanged -= OnConnectionGeometryChanged;
        Redraw();
    }

    void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FlowEditorViewModel.SelectedConnection)
            or nameof(FlowEditorViewModel.IsEditing)
            or nameof(FlowEditorViewModel.Connections))
        {
            if (_connection is not null)
                _connection.GeometryChanged -= OnConnectionGeometryChanged;
            Redraw();
        }
    }

    void OnConnectionGeometryChanged(object? sender, EventArgs e) => Redraw();

    /// <summary>Deixa cliques passarem para o Nodify exceto sobre handles de waypoint.</summary>
    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        if (Root.Children.Count == 0)
            return null!;

        var point = hitTestParameters.HitPoint;
        foreach (UIElement child in Root.Children)
        {
            if (child.InputHitTest(point) is not null)
                return new PointHitTestResult(child, point);
        }

        return null!;
    }

    void Redraw()
    {
        Root.Children.Clear();
        _dragIndex = -1;
        _dragHistoryPushed = false;
        if (_vm is null || !_vm.IsEditing || _vm.SelectedConnection is not { } conn)
        {
            _connection = null;
            return;
        }

        if (!ReferenceEquals(_connection, conn))
        {
            if (_connection is not null)
                _connection.GeometryChanged -= OnConnectionGeometryChanged;
            _connection = conn;
            conn.GeometryChanged += OnConnectionGeometryChanged;
        }

        if (conn.HasManualWaypoints)
            DrawWaypointPath(conn);

        for (var i = 0; i < conn.Waypoints.Count; i++)
        {
            var idx = i;
            var thumb = CreateThumb(conn.Waypoints[i]);
            thumb.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount == 2)
                {
                    _vm?.RemoveWaypoint(conn, idx);
                    Redraw();
                    e.Handled = true;
                    return;
                }
                _dragIndex = idx;
                if (!_dragHistoryPushed)
                {
                    _vm?.BeginWaypointDrag();
                    _dragHistoryPushed = true;
                }
                thumb.CaptureMouse();
                e.Handled = true;
            };
            thumb.MouseLeftButtonUp += (_, e) =>
            {
                if (_dragIndex == idx)
                {
                    _dragIndex = -1;
                    thumb.ReleaseMouseCapture();
                }
                e.Handled = true;
            };
            Root.Children.Add(thumb);
        }

        var handles = conn.HandlePoints;
        for (var seg = 0; seg < handles.Count - 1; seg++)
        {
            var segment = seg;
            var a = handles[seg];
            var b = handles[seg + 1];
            var plus = CreateInsertButton(new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2));
            plus.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;
                _vm?.InsertWaypoint(conn, segment);
                Redraw();
            };
            Root.Children.Add(plus);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragIndex < 0 || _vm is null || _connection is null)
            return;
        if (Mouse.Captured is not Ellipse)
            return;
        var pos = e.GetPosition(Root);
        _vm.MoveWaypoint(_connection, _dragIndex, pos);
        Redraw();
    }

    void DrawWaypointPath(FlowConnectionViewModel conn)
    {
        var path = new Polyline
        {
            Points = conn.PolylinePoints,
            Stroke = new SolidColorBrush(Color.FromArgb(0x66, 0x25, 0x63, 0xEB)),
            StrokeThickness = 2,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false,
        };
        Root.Children.Insert(0, path);
    }

    static Ellipse CreateThumb(Point p)
    {
        var el = new Ellipse
        {
            Width = 11,
            Height = 11,
            Fill = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)),
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Cursor = Cursors.Hand,
            IsHitTestVisible = true,
            ToolTip = "Arraste para ajustar · duplo clique para remover",
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(0x0F, 0x17, 0x2A),
                Opacity = 0.35,
                BlurRadius = 3,
                ShadowDepth = 1,
            },
        };
        Canvas.SetLeft(el, p.X - 5.5);
        Canvas.SetTop(el, p.Y - 5.5);
        return el;
    }

    static Grid CreateInsertButton(Point p)
    {
        var label = new TextBlock
        {
            Text = "+",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, -1, 0, 0),
            IsHitTestVisible = false,
        };
        var ring = new Ellipse
        {
            Width = 14,
            Height = 14,
            Fill = Brushes.White,
            Stroke = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 2, 2 },
            IsHitTestVisible = false,
        };
        var grid = new Grid
        {
            Width = 14,
            Height = 14,
            IsHitTestVisible = true,
            Cursor = Cursors.Hand,
            ToolTip = "Clique para criar um ponto de passagem",
        };
        grid.Children.Add(ring);
        grid.Children.Add(label);
        Canvas.SetLeft(grid, p.X - 7);
        Canvas.SetTop(grid, p.Y - 7);
        return grid;
    }
}
