using System.Windows;
using System.Windows.Threading;

namespace Hoblio.Studio;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
    }

    static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            e.Exception.Message + Environment.NewLine + Environment.NewLine + e.Exception.StackTrace,
            "Hoblio Studio — erro inesperado",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception ex)
            return;
        MessageBox.Show(
            ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace,
            "Hoblio Studio — erro fatal",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}

