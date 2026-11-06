using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Hoblio.Model;

namespace Hoblio.Runtime;

public sealed record UserInputResult(string UserInput, string ButtonPressed);

public sealed record SelectFromListResult(string SelectedItem, string SelectedIndex, string ButtonPressed);

public static class NativeDialogs
{
    /// <summary>
    /// Blocks until the user closes the dialog or the optional timeout elapses (same as Hoblio <c>run_show_message</c>).
    /// A janela é <see cref="Window.Topmost"/> de propósito: o fluxo costuma terminar com o navegador em
    /// primeiro plano, e uma caixa não-topmost pode nascer atrás dele — o run ficaria parado invisível.
    /// </summary>
    public static void RunShowMessage(string title, string text, double? timeoutSeconds)
    {
        RunSta(() =>
        {
            var message = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 560,
            };
            var ok = new Button
            {
                Content = "OK",
                Width = 96,
                Margin = new Thickness(0, 16, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                IsDefault = true,
                IsCancel = true,
            };
            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(message);
            panel.Children.Add(ok);

            var win = new Window
            {
                Title = title,
                Content = panel,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };
            ok.Click += (_, _) => win.Close();

            DispatcherTimer? timer = null;
            if (timeoutSeconds is > 0)
            {
                timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(timeoutSeconds.Value) };
                timer.Tick += (_, _) =>
                {
                    timer!.Stop();
                    win.Close();
                };
                timer.Start();
            }

            win.Closed += (_, _) => timer?.Stop();
            win.ShowDialog();
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
