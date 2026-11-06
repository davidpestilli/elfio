using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Hoblio.Model;
using Hoblio.Runtime;

namespace Hoblio.Studio;

/// <summary>
/// Abre um Chrome/Edge com depuração (CDP) num perfil dedicado — o serviço do botão do ribbon e do
/// atalho no inspector de <c>browser_attach_chrome</c> / <c>browser_attach_edge</c>.
/// A janela é <b>não modal</b> de propósito: o humano abre o navegador, loga, roda o fluxo e volta
/// aqui para conferir as abas, sem fechar nada.
/// </summary>
public partial class CdpBrowserWindow : Window
{
    static readonly string[] EngineLabels = ["Chrome", "Edge"];

    bool _busy;

    public CdpBrowserWindow()
    {
        InitializeComponent();
        EngineCombo.ItemsSource = EngineLabels;
        EngineCombo.SelectedIndex = 0;
        PortBox.Text = CdpBrowser.DefaultPort.ToString(CultureInfo.InvariantCulture);
        UrlBox.Text = "about:blank";
        ProfileBox.Text = CdpBrowser.DefaultProfileDir;
    }

    /// <summary>Preenche os campos a partir do nó selecionado (ou mantém o que já está digitado).</summary>
    public void Preset(string? engine, int? port)
    {
        if (!string.IsNullOrWhiteSpace(engine))
        {
            var isEdge = string.Equals(engine, WebAutomationEnums.BrowserEngine.Edge, StringComparison.OrdinalIgnoreCase);
            EngineCombo.SelectedIndex = isEdge ? 1 : 0;
        }
        if (port is > 0 and <= 65_535)
            PortBox.Text = port.Value.ToString(CultureInfo.InvariantCulture);
    }

    string SelectedEngine =>
        EngineCombo.SelectedIndex == 1
            ? WebAutomationEnums.BrowserEngine.Edge
            : WebAutomationEnums.BrowserEngine.Chrome;

    async void OpenClick(object sender, RoutedEventArgs e)
    {
        if (_busy)
            return;
        if (TryReadPort() is not { } port)
            return;

        var engine = SelectedEngine;
        var url = UrlBox.Text.Trim();
        var profile = ProfileBox.Text.Trim();

        SetBusy(true);
        SetStatus("Abrindo o navegador e esperando o CDP responder…", error: false);
        try
        {
            var session = await Task.Run(() => CdpBrowser.Launch(engine, port, url, profile));
            ShowSession(session);
        }
        catch (Exception ex)
        {
            TabsList.ItemsSource = null;
            SetStatus(RunErrors.CliMessage(ex), error: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void CheckClick(object sender, RoutedEventArgs e)
    {
        if (_busy)
            return;
        if (TryReadPort() is not { } port)
            return;

        SetBusy(true);
        try
        {
            var tabs = await Task.Run(() => CdpBrowser.ListTabs(port));
            var version = await Task.Run(() => CdpBrowser.TryGetVersion(port));
            if (version is null)
            {
                TabsList.ItemsSource = null;
                SetStatus($"Nada respondendo em http://127.0.0.1:{port}. Use “Abrir navegador”.", error: true);
            }
            else
            {
                ShowTabs(tabs);
                SetStatus($"Sessão CDP ativa em http://127.0.0.1:{port} ({tabs.Count} aba(s)).", error: false);
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    void CloseClick(object sender, RoutedEventArgs e) => Close();

    int? TryReadPort()
    {
        if (int.TryParse(PortBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
            && port is > 0 and <= 65_535)
        {
            return port;
        }
        SetStatus("Porta CDP inválida: use um número entre 1 e 65535 (o padrão é 9222).", error: true);
        return null;
    }

    void ShowSession(CdpBrowserSession session)
    {
        ShowTabs(session.Tabs);
        var where = session.ReusedRunningBrowser
            ? "sessão existente reaproveitada"
            : "navegador aberto";
        var label = string.IsNullOrWhiteSpace(session.BrowserLabel) ? session.Engine : session.BrowserLabel;
        SetStatus(
            $"{label} em http://127.0.0.1:{session.Port} — {where}. "
            + "Faça o login nessa janela e rode o fluxo; o navegador não é fechado no fim do run.",
            error: false);
    }

    void ShowTabs(IReadOnlyList<CdpTab> tabs)
    {
        TabsList.ItemsSource = tabs
            .Select(t => string.IsNullOrWhiteSpace(t.Title) ? t.Url : $"{t.Title} — {t.Url}")
            .ToList();
    }

    void SetBusy(bool busy)
    {
        _busy = busy;
        OpenButton.IsEnabled = !busy;
        CheckButton.IsEnabled = !busy;
    }

    void SetStatus(string text, bool error)
    {
        StatusText.Text = text;
        StatusText.Foreground = new SolidColorBrush(error
            ? Color.FromRgb(0xB9, 0x1C, 0x1C)
            : Color.FromRgb(0x47, 0x55, 0x69));
    }
}
