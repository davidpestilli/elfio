using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Hoblio.Studio.ViewModels;
using Nodify;

namespace Hoblio.Studio;

public partial class MainWindow : Window
{
    readonly FlowEditorViewModel _vm = new();
    Point _panMouseStart;
    Point _panViewportStart;
    bool _isViewportPanning;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += (_, _) => HookFlowEditor();
    }

    void HookFlowEditor()
    {
        ConfigureNodeGestures();
        FlowEditor.SelectionChanged += (_, _) =>
            _vm.ApplyCanvasSelection(_vm.DiagramSelectedItems);
    }

    static void ConfigureNodeGestures()
    {
        var container = new EditorGestures.ItemContainerGestures();
        container.Selection.Apply(new EditorGestures.SelectionGestures(MouseAction.LeftClick));
        EditorGestures.Mappings.ItemContainer.Apply(container);
    }

    void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_vm.ConfirmDiscardChanges())
            return;
        e.Cancel = true;
    }

    void Connection_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: FlowConnectionViewModel connection })
            _vm.SelectConnectionCommand.Execute(connection);
    }

    void NodeCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: FlowNodeViewModel node })
            return;
        FlowEditor.Focus();
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            _vm.ToggleNodeInSelection(node);
        else
            _vm.SelectSingleNode(node);
    }

    static bool IsTextInputFocused() =>
        Keyboard.FocusedElement is TextBoxBase or PasswordBox or ComboBox { IsEditable: true };

    void FlowCanvasHost_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsTextInputFocused())
            return;

        if (e.Key == Key.Space)
            FlowCanvasHost.Cursor = Cursors.Hand;

        if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control && _vm.CanEditDiagram)
        {
            _vm.SelectAllDiagramNodes();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            if (_vm.CopySelectionCommand.CanExecute(null))
            {
                _vm.CopySelectionCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.X)
        {
            if (_vm.CutSelectionCommand.CanExecute(null))
            {
                _vm.CutSelectionCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
        {
            if (_vm.PasteCommand.CanExecute(null))
            {
                _vm.PasteCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    void FlowCanvasHost_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && !_isViewportPanning)
            FlowCanvasHost.Cursor = Cursors.Arrow;
    }

    void FlowCanvasHost_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsTextInputFocused())
            return;

        if (IsPanGesture(e))
        {
            StartPan(e);
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left && IsBackgroundClick(e.OriginalSource as DependencyObject))
            _vm.SelectedConnection = null;
    }

    void FlowCanvasHost_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isViewportPanning)
            return;
        var pos = e.GetPosition(FlowCanvasHost);
        var delta = pos - _panMouseStart;
        var zoom = FlowEditor.ViewportZoom;
        FlowEditor.ViewportLocation = new Point(
            _panViewportStart.X - delta.X / zoom,
            _panViewportStart.Y - delta.Y / zoom);
        e.Handled = true;
    }

    void FlowCanvasHost_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isViewportPanning)
            return;
        if (e.ChangedButton is not (MouseButton.Left or MouseButton.Middle or MouseButton.Right))
            return;
        _isViewportPanning = false;
        FlowCanvasHost.ReleaseMouseCapture();
        if (!Keyboard.IsKeyDown(Key.Space))
            FlowCanvasHost.Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    void StartPan(MouseButtonEventArgs e)
    {
        _isViewportPanning = true;
        _panMouseStart = e.GetPosition(FlowCanvasHost);
        _panViewportStart = FlowEditor.ViewportLocation;
        FlowCanvasHost.CaptureMouse();
        FlowCanvasHost.Cursor = Cursors.Hand;
    }

    static bool IsPanGesture(MouseButtonEventArgs e) =>
        e.ChangedButton == MouseButton.Middle
        || e.ChangedButton == MouseButton.Right
        || (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.Space));

    static bool IsBackgroundClick(DependencyObject? source)
    {
        var hit = source;
        while (hit is not null)
        {
            if (hit is ItemContainer or Connector or FlowConnectionView)
                return false;
            if (hit is NodifyEditor)
                return true;
            hit = VisualTreeHelper.GetParent(hit);
        }
        return true;
    }
}
