using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Hoblio.Runtime;

public partial class SelectFromListDialog : Window
{
    readonly bool _limitToList;
    readonly bool _allowEmptySelection;
    readonly bool _allowMultipleSelections;
    readonly IReadOnlyList<string> _items;

    public string SelectedItemText { get; private set; } = "";
    public string SelectedIndexText { get; private set; } = "-1";
    public bool Confirmed { get; private set; }

    public SelectFromListDialog(
        string title,
        string message,
        IReadOnlyList<string> items,
        bool topmost,
        bool limitToList,
        bool allowEmptySelection,
        bool allowMultipleSelections)
    {
        InitializeComponent();
        Title = title;
        MessageBlock.Text = message;
        _items = items;
        _limitToList = limitToList;
        _allowEmptySelection = allowEmptySelection;
        _allowMultipleSelections = allowMultipleSelections;
        if (topmost)
            Topmost = true;

        if (_allowMultipleSelections)
        {
            MultiChoiceList.Visibility = Visibility.Visible;
            MultiChoiceList.ItemsSource = _items;
            MultiChoiceList.SelectionMode = SelectionMode.Extended;
            if (_items.Count > 0)
                MultiChoiceList.SelectedIndex = 0;
        }
        else
        {
            SingleChoiceCombo.Visibility = Visibility.Visible;
            SingleChoiceCombo.ItemsSource = _items;
            SingleChoiceCombo.IsEditable = !_limitToList;
            if (_items.Count > 0)
            {
                SingleChoiceCombo.SelectedIndex = 0;
                if (!_limitToList)
                    SingleChoiceCombo.Text = _items[0];
            }
        }
    }

    void OkClick(object sender, RoutedEventArgs e)
    {
        if (_allowMultipleSelections)
            CommitMultiple();
        else
            CommitSingle();

        if (!Confirmed)
            return;

        DialogResult = true;
    }

    void CommitSingle()
    {
        string value;
        if (_limitToList)
        {
            if (SingleChoiceCombo.SelectedItem is not string selected)
            {
                value = "";
            }
            else
            {
                value = selected;
            }
        }
        else
        {
            value = SingleChoiceCombo.Text.Trim();
        }

        if (string.IsNullOrEmpty(value))
        {
            if (!_allowEmptySelection)
            {
                MessageBox.Show(this, "Selecione um item da lista.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedItemText = "";
            SelectedIndexText = "-1";
            Confirmed = true;
            return;
        }

        if (_limitToList && !_items.Contains(value))
        {
            MessageBox.Show(this, "O valor deve estar na lista.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var index = IndexOfItem(value);
        SelectedItemText = value;
        SelectedIndexText = index >= 0 ? index.ToString() : "-1";
        Confirmed = true;
    }

    void CommitMultiple()
    {
        var selected = MultiChoiceList.SelectedItems.Cast<string>().ToList();
        if (selected.Count == 0)
        {
            if (!_allowEmptySelection)
            {
                MessageBox.Show(this, "Selecione um ou mais itens da lista.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedItemText = "[]";
            SelectedIndexText = "[]";
            Confirmed = true;
            return;
        }

        var indices = selected.Select(IndexOfItem).Where(i => i >= 0).ToList();
        SelectedItemText = System.Text.Json.JsonSerializer.Serialize(selected);
        SelectedIndexText = System.Text.Json.JsonSerializer.Serialize(indices);
        Confirmed = true;
    }

    int IndexOfItem(string item)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i] == item)
                return i;
        }
        return -1;
    }
}
