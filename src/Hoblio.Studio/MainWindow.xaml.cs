using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Hoblio.Studio.ViewModels;

namespace Hoblio.Studio;

public partial class MainWindow : Window
{
    readonly FlowEditorViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += (_, _) => FlowCanvas.AttachViewModel(_vm);
    }

    void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_vm.ConfirmDiscardChanges())
            return;
        e.Cancel = true;
    }

    static bool IsTextInputFocused() =>
        Keyboard.FocusedElement is TextBoxBase or PasswordBox or ComboBox { IsEditable: true };

    void FlowCanvasHost_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsTextInputFocused())
            return;

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
}
