using System.Windows;
using Hoblio.Model;

namespace Hoblio.Runtime;

public sealed record UserInputResult(string UserInput, string ButtonPressed);

public static class NativeDialogs
{
    public static void SpawnShowMessage(string title, string text, double? timeoutSeconds)
    {
        ThreadPool.QueueUserWorkItem(_ =>
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
