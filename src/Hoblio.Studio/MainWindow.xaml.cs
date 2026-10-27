using System.Windows;
using Hoblio.Studio.ViewModels;

namespace Hoblio.Studio;

public partial class MainWindow : Window
{
    readonly FlowEditorViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_vm.ConfirmDiscardChanges())
            return;
        e.Cancel = true;
    }
}
