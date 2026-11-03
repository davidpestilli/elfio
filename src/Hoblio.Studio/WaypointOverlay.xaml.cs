using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Hoblio.Studio.ViewModels;

namespace Hoblio.Studio;

public partial class WaypointOverlay
{
    FlowEditorViewModel? _vm;
    FlowConnectionViewModel? _connection;
    int _dragIndex = -1;

    public WaypointOverlay()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => HookVm();
        Loaded += (_, _) => HookVm();
    }

    void HookVm()
    {
        if (DataContext is not FlowEditorViewModel vm)
            return;
        _vm = vm;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(FlowEditorViewModel.SelectedConnection)
                or nameof(FlowEditorViewModel.IsEditing)
                or nameof(FlowEditorViewModel.Connections))
                Redraw();
        };
        Redraw();
    }

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
        if (_vm is null || !_vm.IsEditing || _vm.SelectedConnection is not { } conn)
            return;
        _connection = conn;
        var points = conn.GetPolylinePoints();
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
        if (conn.Waypoints.Count > 0)
            DrawWaypointPath(conn);

        for (var seg = 0; seg < points.Count - 1; seg++)
        {
            var segment = seg;
            var a = points[seg];
            var b = points[seg + 1];
            var plus = CreateInsertButton(new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2));
            plus.IsHitTestVisible = true;
            plus.Click += (_, _) =>
            {
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
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)),
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Cursor = Cursors.SizeAll,
            IsHitTestVisible = true,
        };
        Canvas.SetLeft(el, p.X - 6);
        Canvas.SetTop(el, p.Y - 6);
        return el;
    }

    static Button CreateInsertButton(Point p)
    {
        var btn = new Button
        {
            Content = "+",
            Width = 16,
            Height = 16,
            Padding = new Thickness(0),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = "Inserir ponto de passagem",
        };
        Canvas.SetLeft(btn, p.X - 8);
        Canvas.SetTop(btn, p.Y - 8);
        return btn;
    }
}
