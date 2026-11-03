using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hoblio.Studio.ViewModels;

namespace Hoblio.Studio;

public partial class FlowConnectionView
{
    public FlowConnectionView()
    {
        InitializeComponent();
    }

    void OnConnectionClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not FlowConnectionViewModel connection)
            return;
        if (Window.GetWindow(this)?.DataContext is FlowEditorViewModel vm)
            vm.SelectConnectionCommand.Execute(connection);
        e.Handled = true;
    }
}
