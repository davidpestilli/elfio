using System.Windows;
using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

/// <summary>
/// Serviço do botão “Abrir CDP…”: abre um navegador com depuração para o fluxo se anexar
/// (<c>browser_attach_chrome</c> / <c>browser_attach_edge</c>). O trabalho de verdade está em
/// <see cref="Hoblio.Runtime.CdpBrowser"/>; aqui só há a janela e o pré-preenchimento.
/// </summary>
public sealed partial class FlowEditorViewModel
{
    public RelayCommand OpenCdpBrowserCommand { get; private set; } = null!;

    public RelayCommand OpenCdpBrowserForAttachCommand { get; private set; } = null!;

    CdpBrowserWindow? _cdpBrowserWindow;

    void InitCdpBrowserCommands()
    {
        OpenCdpBrowserCommand = new RelayCommand(() => ShowCdpBrowser(null, null));
        OpenCdpBrowserForAttachCommand = new RelayCommand(OpenCdpBrowserForAttach);
    }

    /// <summary>Atalho do inspector: herda motor e porta do nó de anexar selecionado.</summary>
    void OpenCdpBrowserForAttach()
    {
        if (WebA() is not { } action)
        {
            ShowCdpBrowser(null, null);
            return;
        }

        var engine = action.Type == WebAutomationActionTypes.AttachEdge
            ? WebAutomationEnums.BrowserEngine.Edge
            : WebAutomationEnums.BrowserEngine.Chrome;
        var port = action.DebuggingPort is > 0 and <= 65_535 ? (int)action.DebuggingPort.Value : (int?)null;
        ShowCdpBrowser(engine, port);
    }

    void ShowCdpBrowser(string? engine, int? port)
    {
        if (_cdpBrowserWindow is { IsLoaded: true } open)
        {
            open.Preset(engine, port);
            open.Activate();
            return;
        }

        var window = new CdpBrowserWindow
        {
            Owner = Application.Current?.MainWindow,
        };
        window.Closed += (_, _) => _cdpBrowserWindow = null;
        window.Preset(engine, port);
        _cdpBrowserWindow = window;
        window.Show();
    }
}
