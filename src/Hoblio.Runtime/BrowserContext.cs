using Hoblio.Model;
using PuppeteerSharp;

namespace Hoblio.Runtime;

public sealed partial class BrowserContext : IDisposable
{
    const int DefaultTimeoutMs = 10_000;

    IBrowser? _browser;
    IPage? _page;
    bool _ownsBrowser;

    public void Dispose() => EndSession(closeBrowserWindow: _ownsBrowser);

    void EndSession(bool closeBrowserWindow)
    {
        if (_browser is null)
            return;

        if (_ownsBrowser && closeBrowserWindow)
            _browser.CloseAsync().GetAwaiter().GetResult();
        else
            _browser.Disconnect();

        _browser.Dispose();
        _browser = null;
        _page = null;
        _ownsBrowser = false;
    }

    public string Navigate(string url, ulong? waitMs, bool? showOnScreen, bool? startMaximized)
    {
        var visible = showOnScreen ?? true;
        var maximized = startMaximized ?? true;
        if (_page is null)
            LaunchSession(url, visible, maximized, _preferredEngine).GetAwaiter().GetResult();
        else
            NavigateExisting(url).GetAwaiter().GetResult();

        if (waitMs is > 0)
            Thread.Sleep((int)waitMs.Value);

        var title = _page!.GetTitleAsync().GetAwaiter().GetResult();
        return $"browser_navigate: {url} (title=\"{title}\")";
    }

    public string Click(
        string selector,
        ulong? timeoutMs,
        bool waitForNavigation,
        string? waitSelectorAfter,
        ulong? waitAfterTimeoutMs,
        string? dialogAction = null)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var page = RequirePage();

        // resposta automática ao confirm()/alert() que o clique abrir (o renderer trava até isso).
        EventHandler<DialogEventArgs>? onDialog = null;
        if (!string.IsNullOrWhiteSpace(dialogAction))
        {
            var dismiss = string.Equals(dialogAction.Trim(), WebAutomationEnums.DialogAction.Dismiss, StringComparison.OrdinalIgnoreCase);
            onDialog = (_, e) =>
            {
                // fire-and-forget: o handler roda no loop do CDP e precisa devolver o controle.
                try
                {
                    if (dismiss) _ = e.Dialog.Dismiss();
                    else _ = e.Dialog.Accept();
                }
                catch
                {
                    // diálogo já fechado/descartado
                }
            };
            page.Dialog += onDialog;
        }

        try
        {
            RunWithTransientRetry(() =>
            {
                WaitForSelectorAsync(page, selector, timeout).GetAwaiter().GetResult();

                // A espera pela navegação precisa estar ARMADA antes do clique: se o clique
                // navegar rápido (é o caso do eproc), o evento chega antes de alguém escutar,
                // e o `wait_for_navigation` gastaria o timeout inteiro esperando um evento
                // que já passou.
                Task? navTask = waitForNavigation ? WaitForNavigationAsync(page, timeout) : null;

                try
                {
                    page.ClickAsync(selector).GetAwaiter().GetResult();
                    if (navTask is not null)
                        navTask.GetAwaiter().GetResult();
                }
                catch
                {
                    // não deixa a task de navegação pendente virar exceção não observada
                    if (navTask is not null && !navTask.IsCompleted)
                        _ = navTask.ContinueWith(t => _ = t.Exception, TaskScheduler.Default);
                    throw;
                }

                if (waitForNavigation)
                    WaitForDomStableAsync(page, Math.Min(timeout, 8000)).GetAwaiter().GetResult();

                if (waitSelectorAfter is not null)
                    WaitForSelectorAsync(page, waitSelectorAfter, (int)(waitAfterTimeoutMs ?? (ulong)timeout)).GetAwaiter().GetResult();
            });
        }
        finally
        {
            if (onDialog is not null)
            {
                try { page.Dialog -= onDialog; } catch { /* página pode ter fechado */ }
            }
        }

        var notes = (waitForNavigation, waitSelectorAfter is not null) switch
        {
            (true, true) => " (navigation + selector)",
            (true, false) => " (waited for navigation)",
            (false, true) => " (waited for selector)",
            _ => "",
        };
        if (onDialog is not null)
            notes += $" (dialog={dialogAction})";
        return $"browser_click: {selector}{notes}";
    }

    public string WaitNavigation(ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var page = RequirePage();
        RunWithTransientRetry(() =>
        {
            WaitForNavigationAsync(page, timeout).GetAwaiter().GetResult();
            WaitForDomStableAsync(page, Math.Min(timeout, 8000)).GetAwaiter().GetResult();
        });
        return $"browser_wait_navigation: ok ({timeout}ms max)";
    }

    public string Fill(string selector, string text, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var page = RequirePage();
        RunWithTransientRetry(() =>
        {
            var handle = WaitForSelectorAsync(page, selector, timeout).GetAwaiter().GetResult();
            handle.ClickAsync().GetAwaiter().GetResult();
            handle.EvaluateFunctionAsync(
                @"el => {
                if (el.value !== undefined) el.value = '';
                else if (el.isContentEditable) el.textContent = '';
            }").GetAwaiter().GetResult();
            handle.TypeAsync(text).GetAwaiter().GetResult();
        });
        return $"browser_fill: {selector} ({text.Length} chars)";
    }

    public string WaitFor(string selector, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        RunWithTransientRetry(() =>
            WaitForSelectorAsync(RequirePage(), selector, timeout).GetAwaiter().GetResult());
        return $"browser_wait_for: {selector}";
    }

    public string GetText(string selector, ulong? timeoutMs, string? variable, RunVariables vars)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var text = RunWithTransientRetry(() =>
        {
            var page = RequirePage();
            var handle = WaitForSelectorAsync(page, selector, timeout).GetAwaiter().GetResult();
            return handle.EvaluateFunctionAsync<string>(
                "el => (el && (el.innerText || el.textContent)) ? (el.innerText || el.textContent || '').trim() : ''"
            ).GetAwaiter().GetResult() ?? "";
        });

        var varNote = "";
        if (!string.IsNullOrEmpty(variable))
        {
            vars[variable] = text;
            varNote = $" (variable `{variable}` set)";
        }
        var preview = text.Length > 80 ? text[..80] : text;
        var ellipsis = text.Length > 80 ? "…" : "";
        return $"browser_get_text: {selector} -> {preview}{varNote}{ellipsis}";
    }

    IPage RequirePage() =>
        _page ?? throw new InvalidOperationException("no browser session: run browser_navigate before other browser actions");

    static void RunWithTransientRetry(Action action, int attempts = 4)
    {
        Exception? last = null;
        for (var i = 0; i < attempts; i++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception ex) when (IsTransientBrowserError(ex) && i < attempts - 1)
            {
                last = ex;
                Thread.Sleep(150 * (i + 1));
            }
        }

        if (last is not null)
            throw last;
        action();
    }

    static T RunWithTransientRetry<T>(Func<T> action, int attempts = 4)
    {
        Exception? last = null;
        for (var i = 0; i < attempts; i++)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (IsTransientBrowserError(ex) && i < attempts - 1)
            {
                last = ex;
                Thread.Sleep(150 * (i + 1));
            }
        }

        if (last is not null)
            throw last;
        return action();
    }

    static bool IsTransientBrowserError(Exception ex)
    {
        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            var msg = cur.Message;
            if (msg.Contains("Execution context was destroyed", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Cannot find context with specified id", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Inspected target navigated or closed", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Target closed", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Protocol error", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    async Task LaunchSession(string url, bool showOnScreen, bool startMaximized, string? browserEngine = null)
    {
        var exe = ChromiumLocator.ResolveExecutable(browserEngine ?? _preferredEngine);
        var args = new List<string>
        {
            "--remote-allow-origins=*",
            "--disable-background-networking",
        };
        if (showOnScreen && startMaximized)
            args.Add("--start-maximized");

        var options = new LaunchOptions
        {
            ExecutablePath = exe,
            Headless = !showOnScreen,
            Args = args.ToArray(),
            DefaultViewport = showOnScreen ? null : new ViewPortOptions { Width = 1280, Height = 720 },
        };
        _browser = await Puppeteer.LaunchAsync(options);
        _ownsBrowser = true;
        _page = await _browser.NewPageAsync();
        await GoToCompatAsync(_page, url);
    }

    async Task NavigateExisting(string url) => await GoToCompatAsync(_page!, url);

    static async Task GoToCompatAsync(IPage page, string url)
    {
        var options = new NavigationOptions
        {
            WaitUntil = [WaitUntilNavigation.Networkidle2],
            ReferrerPolicy = "strict-origin-when-cross-origin",
        };
        await page.GoToAsync(url, options);
        await WaitForDomStableAsync(page, 5000);
    }

    static async Task<IElementHandle> WaitForSelectorAsync(IPage page, string selector, int timeoutMs, bool visible = false)
    {
        var handle = await page.WaitForSelectorAsync(
            selector,
            new WaitForSelectorOptions
            {
                Timeout = Math.Max(1, timeoutMs),
                Visible = visible,
            });
        if (handle is null)
            throw new TimeoutException($"timeout ({timeoutMs}ms) waiting for selector `{selector}`");
        return handle;
    }

    static async Task WaitForNavigationAsync(IPage page, int timeoutMs)
    {
        if (await PageIsStableAsync(page))
            return;

        try
        {
            await page.WaitForNavigationAsync(new NavigationOptions
            {
                Timeout = Math.Max(1, timeoutMs),
                WaitUntil = [WaitUntilNavigation.Load, WaitUntilNavigation.DOMContentLoaded],
            });
        }
        catch (TimeoutException ex)
        {
            if (await PageIsStableAsync(page))
                return;
            throw new TimeoutException($"timeout ({timeoutMs}ms) waiting for navigation", ex);
        }
    }

    static async Task WaitForDomStableAsync(IPage page, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(1, timeoutMs));
        var stableSince = DateTime.MinValue;
        while (DateTime.UtcNow < deadline)
        {
            if (await PageIsStableAsync(page))
            {
                if (stableSince == DateTime.MinValue)
                    stableSince = DateTime.UtcNow;
                if ((DateTime.UtcNow - stableSince).TotalMilliseconds >= 200)
                    return;
            }
            else
                stableSince = DateTime.MinValue;

            await Task.Delay(100);
        }
    }

    static async Task<bool> PageIsStableAsync(IPage page)
    {
        try
        {
            var state = await page.EvaluateFunctionAsync<string>("() => document.readyState");
            return state is "complete" or "interactive";
        }
        catch (Exception ex) when (IsTransientBrowserError(ex))
        {
            return false;
        }
    }
}
