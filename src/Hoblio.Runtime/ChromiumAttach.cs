using System.Net.Http;
using Hoblio.Model;
using PuppeteerSharp;

namespace Hoblio.Runtime;

static class ChromiumAttach
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(3) };

    public static async Task<(IBrowser Browser, IPage Page, int Port)> ConnectAndPickPageAsync(
        string browserEngine,
        string attachMode,
        string? urlFragment,
        string? titleFragment,
        int? debuggingPort,
        CancellationToken cancel = default)
    {
        var errors = new List<string>();
        foreach (var endpoint in ListEndpoints(browserEngine, debuggingPort))
        {
            try
            {
                if (!await IsDebuggerReachableAsync(endpoint, cancel))
                {
                    errors.Add($"{endpoint}: no response");
                    continue;
                }

                var browser = await Puppeteer.ConnectAsync(new ConnectOptions
                {
                    BrowserURL = endpoint,
                    DefaultViewport = null,
                });

                var pages = (await browser.PagesAsync()).ToList();
                var candidates = new List<ChromiumPageSelection.Candidate>();
                foreach (var page in pages)
                {
                    cancel.ThrowIfCancellationRequested();
                    var url = page.Url ?? "";
                    var title = await page.GetTitleAsync();
                    candidates.Add(new ChromiumPageSelection.Candidate(url, title ?? ""));
                }

                var foregroundTitle = string.Equals(attachMode, WebAutomationEnums.AttachMode.Foreground, StringComparison.OrdinalIgnoreCase)
                    ? Win32Foreground.TryGetChromiumForegroundTitle()
                    : null;

                var picked = ChromiumPageSelection.Pick(candidates, attachMode, urlFragment, titleFragment, foregroundTitle);
                if (picked is null)
                {
                    browser.Disconnect();
                    errors.Add($"{endpoint}: no matching tab");
                    continue;
                }

                IPage? targetPage = null;
                for (var i = 0; i < pages.Count; i++)
                {
                    if (candidates[i].Url == picked.Url && candidates[i].Title == picked.Title)
                    {
                        targetPage = pages[i];
                        break;
                    }
                }

                targetPage ??= pages.FirstOrDefault(p =>
                    string.Equals(p.Url, picked.Url, StringComparison.OrdinalIgnoreCase));

                if (targetPage is null)
                {
                    browser.Disconnect();
                    errors.Add($"{endpoint}: tab disappeared");
                    continue;
                }

                await targetPage.BringToFrontAsync();
                var port = TryParsePortFromEndpoint(endpoint) ?? 0;
                return (browser, targetPage, port);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add($"{endpoint}: {ex.Message}");
            }
        }

        throw new InvalidOperationException(
            "Could not attach to Chromium. Start Chrome or Edge with remote debugging, e.g.\n"
            + "  chrome.exe --remote-debugging-port=9222\n"
            + "  msedge.exe --remote-debugging-port=9222\n"
            + "Or set HOBLIO_BROWSER_DEBUG_PORT / HOBLIO_BROWSER_DEBUG_URL.\n"
            + "Attempts: " + string.Join("; ", errors));
    }

    static async Task<bool> IsDebuggerReachableAsync(string browserUrl, CancellationToken cancel)
    {
        try
        {
            using var response = await Http.GetAsync($"{browserUrl.TrimEnd('/')}/json/version", cancel);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    static List<string> ListEndpoints(string browserEngine, int? debuggingPort)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();

        void Add(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;
            var normalized = url.Trim().TrimEnd('/');
            if (seen.Add(normalized))
                list.Add(normalized);
        }

        Add(Environment.GetEnvironmentVariable("HOBLIO_BROWSER_DEBUG_URL"));
        if (debuggingPort is > 0)
            Add($"http://127.0.0.1:{debuggingPort.Value}");
        if (int.TryParse(Environment.GetEnvironmentVariable("HOBLIO_BROWSER_DEBUG_PORT"), out var envPort) && envPort > 0)
            Add($"http://127.0.0.1:{envPort}");

        var preferEdge = string.Equals(browserEngine, WebAutomationEnums.BrowserEngine.Edge, StringComparison.OrdinalIgnoreCase);
        foreach (var port in preferEdge ? new[] { 9223, 9222, 9333 } : new[] { 9222, 9223, 9333 })
            Add($"http://127.0.0.1:{port}");

        return list;
    }

    static int? TryParsePortFromEndpoint(string endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.Port > 0 ? uri.Port : null;
}
