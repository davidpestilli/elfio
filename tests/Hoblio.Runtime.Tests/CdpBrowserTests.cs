using System.Diagnostics;
using Hoblio.Runtime;

namespace Hoblio.Runtime.Tests;

/// <summary>
/// <see cref="CdpBrowser"/> é o serviço do botão “Abrir navegador CDP” do Studio e do verbo
/// <c>hoblio browser</c>: abre Chrome/Edge com depuração num perfil dedicado para o
/// <c>browser_attach_*</c> encontrar a sessão.
/// </summary>
public class CdpBrowserTests
{
    [Fact]
    public void Default_profile_is_dedicated_and_outside_the_daily_browser()
    {
        var dir = CdpBrowser.DefaultProfileDir;
        Assert.False(string.IsNullOrWhiteSpace(dir));
        Assert.StartsWith(Path.GetTempPath(), dir, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("elfio", dir, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Closed_port_reports_no_cdp_session()
    {
        var port = FindClosedPort();
        Assert.Null(CdpBrowser.TryGetVersion(port));
        Assert.False(CdpBrowser.IsListening(port));
        Assert.Empty(CdpBrowser.ListTabs(port));
    }

    /// <summary>Sem navegador aberto, o attach do fluxo falha com instrução — não com "no response" seco.</summary>
    [Fact]
    public void Invalid_port_is_rejected_before_touching_the_browser()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CdpBrowser.Launch("chrome", 0, "about:blank"));
        Assert.Contains("porta CDP", ex.Message);
    }

    /// <summary>
    /// Abre um navegador de verdade. Fora do gate <c>HOBLIO_BROWSER_TEST=1</c> não roda (mesmo critério
    /// de <see cref="BrowserNavigateTests"/>: CI sem Chrome não pode quebrar).
    /// </summary>
    [Fact]
    public void Launch_starts_a_cdp_session_when_browser_tests_are_enabled()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("HOBLIO_BROWSER_TEST"), "1", StringComparison.Ordinal))
            return;

        var port = FindClosedPort();
        var profile = Path.Combine(Path.GetTempPath(), "elfio-cdp-test-" + Guid.NewGuid().ToString("N")[..8]);
        var session = CdpBrowser.Launch("chrome", port, "about:blank", profile);
        try
        {
            Assert.Equal(port, session.Port);
            Assert.True(session.ProcessId > 0);
            Assert.False(session.ReusedRunningBrowser);
            Assert.False(string.IsNullOrWhiteSpace(session.BrowserLabel));
            Assert.True(CdpBrowser.IsListening(port));
        }
        finally
        {
            try
            {
                Process.GetProcessById(session.ProcessId).Kill(entireProcessTree: true);
            }
            catch
            {
                // já saiu (ou é o processo que apenas repassou a URL para uma instância viva)
            }
            try
            {
                Directory.Delete(profile, recursive: true);
            }
            catch
            {
                // perfil em uso pelo navegador que ainda está fechando
            }
        }
    }

    /// <summary>Porta alta que ninguém está escutando (tenta algumas para não brigar com outro teste).</summary>
    static int FindClosedPort()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var port = 9470 + Random.Shared.Next(0, 200);
            if (!CdpBrowser.IsListening(port))
                return port;
        }
        throw new InvalidOperationException("não encontrei porta livre para o teste de CDP");
    }
}
