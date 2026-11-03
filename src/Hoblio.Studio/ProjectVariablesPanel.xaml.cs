using System.Windows;
using System.Windows.Controls;
using Hoblio.Studio.ViewModels;

namespace Hoblio.Studio;

public partial class ProjectVariablesPanel
{
    public ProjectVariablesPanel() => InitializeComponent();

    FlowEditorViewModel? Vm => DataContext as FlowEditorViewModel;

    void Row_LostFocus(object sender, RoutedEventArgs e) => Vm?.NotifyProjectVariableRowChanged();

    void ScalarKind_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox box || box.DataContext is not ScalarVariableRow row)
            return;
        foreach (ComboBoxItem item in box.Items)
        {
            if (item.Tag is ScalarVariableKind k && k == row.Kind)
            {
                box.SelectedItem = item;
                break;
            }
        }
    }

    void ScalarKind_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { DataContext: ScalarVariableRow row, SelectedItem: ComboBoxItem item })
            return;
        if (item.Tag is ScalarVariableKind kind)
        {
            row.Kind = kind;
            Vm?.NotifyProjectVariableRowChanged();
        }
    }
}
