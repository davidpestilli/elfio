using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Hoblio.Model;

namespace Hoblio.Runtime;

public sealed record CdpTab(string Title, string Url);

/// <summary>Resultado de <see cref="CdpBrowser.Launch"/>: o que abriu, em que porta e o que já existe lá.</summary>
public sealed record CdpBrowserSession(
    int Port,
    string Engine,
    string Executable,
    string ProfileDir,
    int ProcessId,
    bool ReusedRunningBrowser,
    string? BrowserLabel,
    IReadOnlyList<CdpTab> Tabs);

/// <summary>
/// Abre um Chrome/Edge **com porta de depuração** (CDP) num perfil dedicado, para um fluxo se
/// anexar (<c>browser_attach_chrome</c> / <c>browser_attach_edge</c>) à sessão em que o humano está
/// logado — o pré-requisito do "modo individual" de fluxos que assumem uma tela já aberta.
///
/// Por que não dá para usar o navegador "normal": ele não expõe porta de debug, e desde o
/// Chrome/Edge 136 a flag <c>--remote-debugging-port</c> é **ignorada no perfil padrão** (proteção
/// contra roubo de cookies). Daí o <c>--user-data-dir</c> próprio, que também preserva o login
/// entre execuções.
/// </summary>
public static class CdpBrowser
{
    public const int DefaultPort = 9222;

    /// <summary>Perfil do humano para automação: separado do perfil diário e reutilizado entre runs.</summary>
    public static string DefaultProfileDir => Path.Combine(Path.GetTempPath(), "elfio-cdp-profile");

    static readonly HttpClient Http = CreateHttp();

    static HttpClient CreateHttp()
    {
        // 127.0.0.1 nunca deve passar por proxy: um proxy no ambiente faria o probe "não responder".
        var handler = new HttpClientHandler { UseProxy = false };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };
    }

    /// <summary>Há um navegador escutando CDP nesta porta?</summary>
    public static bool IsListening(int port) => TryGetVersion(port) is not null;

    /// <summary>Corpo de <c>/json/version</c> (ou <c>null</c> quando ninguém responde).</summary>
    public static string? TryGetVersion(int port) => TryGet($"http://127.0.0.1:{port}/json/version");

    /// <summary>Abas de página abertas no navegador que escuta CDP nesta porta.</summary>
    public static IReadOnlyList<CdpTab> ListTabs(int port)
    {
        var body = TryGet($"http://127.0.0.1:{port}/json/list");
        if (body is null)
            return [];

        var tabs = new List<CdpTab>();
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;
                if (item.TryGetProperty("type", out var type)
                    && type.ValueKind == JsonValueKind.String
                    && !string.Equals(type.GetString(), "page", StringComparison.Ordinal))
                {
                    continue;
                }
                var url = Str(item, "url");
                if (url.Length == 0)
                    continue;
                tabs.Add(new CdpTab(Str(item, "title"), url));
            }
        }
        catch (JsonException)
        {
            return [];
        }
        return tabs;
    }

    /// <summary>
    /// Abre (ou reaproveita) o navegador com CDP e espera o endpoint responder.
    /// Quando já existe um navegador com esse perfil, o processo novo só entrega a URL a ele —
    /// é o que <see cref="CdpBrowserSession.ReusedRunningBrowser"/> reporta.
    /// </summary>
    public static CdpBrowserSession Launch(
        string? engine = null,
        int port = DefaultPort,
        string? url = null,
        string? profileDir = null,
        int waitMs = 20_000)
    {
        if (port is < 1 or > 65_535)
            throw new InvalidOperationException($"porta CDP inválida: {port}");

        var browserEngine = string.IsNullOrWhiteSpace(engine)
            ? WebAutomationEnums.BrowserEngine.Chrome
            : engine.Trim();

        var exe = ChromiumLocator.ResolveExecutable(browserEngine);
        var profile = string.IsNullOrWhiteSpace(profileDir) ? DefaultProfileDir : profileDir.Trim();
        Directory.CreateDirectory(profile);

        var alreadyRunning = IsListening(port);
        var arguments = new List<string>
        {
            $"--remote-debugging-port={port}",
            $"--user-data-dir={profile}",
            "--remote-allow-origins=*",
            "--no-first-run",
            "--no-default-browser-check",
        };
        if (!string.IsNullOrWhiteSpace(url))
            arguments.Add(url.Trim());

        // UseShellExecute=true de proposito: o navegador NAO pode herdar o stdout/stderr de quem
        // chamou. Herdando, o pipe do CLI/Studio (ou de um `dotnet run | Select-Object`) so fecha
        // quando o navegador fecha — o comando ficaria pendurado com a janela aberta.
        var startInfo = new ProcessStartInfo(exe)
        {
            UseShellExecute = true,
            Arguments = string.Join(" ", arguments.Select(QuoteArgument)),
        };
        var started = Process.Start(startInfo);
        var processId = started?.Id ?? 0;
        started?.Dispose();

        var deadline = Environment.TickCount64 + Math.Max(500, waitMs);
        while (Environment.TickCount64 < deadline)
        {
            if (IsListening(port))
                return BuildSession(port, browserEngine, exe, profile, processId, alreadyRunning);
            Thread.Sleep(250);
        }

        throw new InvalidOperationException(
            $"o navegador foi iniciado ({exe}), mas o CDP não respondeu em http://127.0.0.1:{port}. "
            + "Se já havia um navegador aberto com OUTRO perfil, ele pode ter capturado a chamada "
            + "(feche-o e tente de novo) ou a porta está ocupada por outro processo.");
    }

    static CdpBrowserSession BuildSession(
        int port,
        string engine,
        string executable,
        string profileDir,
        int processId,
        bool reusedRunningBrowser)
    {
        var version = TryGetVersion(port);
        return new CdpBrowserSession(
            port,
            engine,
            executable,
            profileDir,
            processId,
            reusedRunningBrowser,
            BrowserLabelFrom(version),
            ListTabs(port));
    }

    static string? BrowserLabelFrom(string? versionJson)
    {
        if (string.IsNullOrWhiteSpace(versionJson))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(versionJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var browser = Str(doc.RootElement, "Browser");
                if (browser.Length > 0)
                    return browser;
            }
        }
        catch (JsonException)
        {
            // resposta inesperada: o rótulo é enfeite, não vale falhar por causa dele
        }
        return null;
    }

    static string? TryGet(string url)
    {
        try
        {
            using var response = Http.GetAsync(url).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                return null;
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    static string Str(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    /// <summary>Aspas só quando necessário (caminho de perfil com espaço, por exemplo).</summary>
    static string QuoteArgument(string argument) =>
        argument.Length > 0 && argument.IndexOfAny([' ', '\t', '"']) < 0
            ? argument
            : "\"" + argument.Replace("\"", "\\\"") + "\"";
}
