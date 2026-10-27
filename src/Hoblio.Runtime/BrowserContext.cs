using PuppeteerSharp;

namespace Hoblio.Runtime;

public sealed class BrowserContext : IDisposable
{
    const int DefaultTimeoutMs = 10_000;

    IBrowser? _browser;
    IPage? _page;

    public void Dispose()
    {
        _browser?.CloseAsync().GetAwaiter().GetResult();
        _browser?.Dispose();
        _browser = null;
        _page = null;
    }

    public string Navigate(string url, ulong? waitMs, bool? showOnScreen, bool? startMaximized)
    {
        var visible = showOnScreen ?? true;
        var maximized = startMaximized ?? true;
        if (_page is null)
            LaunchSession(url, visible, maximized).GetAwaiter().GetResult();
        else
            NavigateExisting(url).GetAwaiter().GetResult();

        if (waitMs is > 0)
            Thread.Sleep((int)waitMs.Value);

        var title = _page!.GetTitleAsync().GetAwaiter().GetResult();
        return $"browser_navigate: {url} (title=\"{title}\")";
    }

    public string Click(string selector, ulong? timeoutMs, bool waitForNavigation, string? waitSelectorAfter, ulong? waitAfterTimeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var page = RequirePage();
        WaitForSelectorAsync(page, selector, timeout).GetAwaiter().GetResult();
        page.ClickAsync(selector).GetAwaiter().GetResult();
        if (waitForNavigation)
            WaitForNavigationAsync(page, timeout).GetAwaiter().GetResult();
        if (waitSelectorAfter is not null)
            WaitForSelectorAsync(page, waitSelectorAfter, (int)(waitAfterTimeoutMs ?? (ulong)timeout)).GetAwaiter().GetResult();
        var notes = (waitForNavigation, waitSelectorAfter is not null) switch
        {
            (true, true) => " (navigation + selector)",
            (true, false) => " (waited for navigation)",
            (false, true) => " (waited for selector)",
            _ => "",
        };
        return $"browser_click: {selector}{notes}";
    }

    public string WaitNavigation(ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        WaitForNavigationAsync(RequirePage(), timeout).GetAwaiter().GetResult();
        return $"browser_wait_navigation: ok ({timeout}ms max)";
    }

    public string Fill(string selector, string text, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var page = RequirePage();
        WaitForSelectorAsync(page, selector, timeout).GetAwaiter().GetResult();
        page.FocusAsync(selector).GetAwaiter().GetResult();
        page.EvaluateFunctionAsync(
            @"sel => {
                const el = document.querySelector(sel);
                if (!el) return;
                if (el.value !== undefined) el.value = '';
                else if (el.isContentEditable) el.textContent = '';
            }",
            selector).GetAwaiter().GetResult();
        page.TypeAsync(selector, text).GetAwaiter().GetResult();
        return $"browser_fill: {selector} ({text.Length} chars)";
    }

    public string WaitFor(string selector, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        WaitForSelectorAsync(RequirePage(), selector, timeout).GetAwaiter().GetResult();
        return $"browser_wait_for: {selector}";
    }

    public string GetText(string selector, ulong? timeoutMs, string? variable, RunVariables vars)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var page = RequirePage();
        WaitForSelectorAsync(page, selector, timeout).GetAwaiter().GetResult();
        var text = page.EvaluateFunctionAsync<string>(
            "sel => { const el = document.querySelector(sel); return el ? (el.innerText || el.textContent || '') : ''; }",
            selector).GetAwaiter().GetResult() ?? "";

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

    async Task LaunchSession(string url, bool showOnScreen, bool startMaximized)
    {
        var exe = ChromiumLocator.ResolveExecutable();
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
        _page = await _browser.NewPageAsync();
        await GoToCompatAsync(_page, url);
    }

    async Task NavigateExisting(string url)
    {
        await GoToCompatAsync(_page!, url);
    }

    static async Task GoToCompatAsync(IPage page, string url)
    {
        var options = new NavigationOptions
        {
            WaitUntil = [WaitUntilNavigation.Networkidle2],
            ReferrerPolicy = "strict-origin-when-cross-origin",
        };
        await page.GoToAsync(url, options);
    }

    static async Task WaitForSelectorAsync(IPage page, string selector, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(1, timeoutMs));
        while (true)
        {
            var el = await page.QuerySelectorAsync(selector);
            if (el is not null)
                return;
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException($"timeout ({timeoutMs}ms) waiting for selector `{selector}`");
            await Task.Delay(100);
        }
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

    static async Task<bool> PageIsStableAsync(IPage page)
    {
        var state = await page.EvaluateFunctionAsync<string>("() => document.readyState");
        return state is "complete" or "interactive";
    }
}
