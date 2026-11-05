using System.Windows;
using Hoblio.Model;

namespace Hoblio.Runtime;

public sealed record UserInputResult(string UserInput, string ButtonPressed);

public sealed record SelectFromListResult(string SelectedItem, string SelectedIndex, string ButtonPressed);

public static class NativeDialogs
{
    /// <summary>Blocks until the user closes the dialog or the optional timeout elapses (same as Hoblio <c>run_show_message</c>).</summary>
    public static void RunShowMessage(string title, string text, double? timeoutSeconds)
    {
        RunSta(() =>
        {
            if (timeoutSeconds is > 0)
            {
                var win = new Window
                {
                    Title = title,
                    Width = 420,
                    Height = 200,
                    Topmost = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new System.Windows.Controls.TextBlock
                    {
                        Text = text,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(16),
                    },
                };
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(timeoutSeconds.Value),
                };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    win.Close();
                };
                timer.Start();
                win.ShowDialog();
            }
            else
            {
                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        });
    }

    public static UserInputResult RunShowGetUserInput(
        string title,
        string message,
        string defaultValue,
        UserInputKind inputKind,
        bool topmost)
    {
        UserInputResult? result = null;
        RunSta(() =>
        {
            var dlg = new UserInputDialog(title, message, defaultValue, inputKind, topmost);
            if (dlg.ShowDialog() == true)
                result = new UserInputResult(dlg.InputText, "OK");
            else
                result = new UserInputResult(dlg.InputText, "Cancel");
        });
        return result ?? new UserInputResult("", "Cancel");
    }

    public static SelectFromListResult RunShowSelectFromList(
        string title,
        string message,
        IReadOnlyList<string> items,
        bool topmost,
        bool limitToList,
        bool allowEmptySelection,
        bool allowMultipleSelections)
    {
        SelectFromListResult? result = null;
        RunSta(() =>
        {
            var dlg = new SelectFromListDialog(
                title,
                message,
                items,
                topmost,
                limitToList,
                allowEmptySelection,
                allowMultipleSelections);
            if (dlg.ShowDialog() == true && dlg.Confirmed)
                result = new SelectFromListResult(dlg.SelectedItemText, dlg.SelectedIndexText, "OK");
            else
                result = new SelectFromListResult(
                    allowMultipleSelections ? "[]" : "",
                    allowMultipleSelections ? "[]" : "-1",
                    "Cancel");
        });
        return result ?? new SelectFromListResult("", "-1", "Cancel");
    }

    static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null)
            throw error;
    }
}
