using System.Windows;
using System.Windows.Controls;
using Hoblio.Studio.ViewModels;

namespace Hoblio.Studio;

public partial class TemplateVariableChips
{
    public static readonly DependencyProperty FieldKeyProperty =
        DependencyProperty.Register(nameof(FieldKey), typeof(string), typeof(TemplateVariableChips), new PropertyMetadata(""));

    public string FieldKey
    {
        get => (string)GetValue(FieldKeyProperty);
        set => SetValue(FieldKeyProperty, value);
    }

    public TemplateVariableChips() => InitializeComponent();

    void Chip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Content: string varName })
            return;
        if (DataContext is not FlowEditorViewModel vm || string.IsNullOrEmpty(FieldKey))
            return;
        vm.InsertTemplateTokenCommand.Execute($"{FieldKey}|{varName}");
    }
}
