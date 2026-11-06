using System.Text;
using System.Text.Json;
using Hoblio.Model;
using PuppeteerSharp;

namespace Hoblio.Runtime;

public sealed partial class BrowserContext
{
    string? _preferredEngine;

    public string Execute(WebAutomationAction action, RunVariables vars, string? projectRoot)
    {
        return action.Type switch
        {
            WebAutomationActionTypes.Navigate => Navigate(
                RequireTemplate(action.Url, vars, "url"),
                action.WaitMs,
                action.ShowOnScreen,
                action.StartMaximized),
            WebAutomationActionTypes.GoTo => GoToOnly(
                RequireTemplate(action.Url, vars, "url"),
                action.WaitMs),
            WebAutomationActionTypes.LaunchChrome => LaunchBrowser(
                WebAutomationEnums.BrowserEngine.Chrome,
                OptionalTemplate(action.Url, vars),
                action.WaitMs,
                action.ShowOnScreen,
                action.StartMaximized),
            WebAutomationActionTypes.LaunchEdge => LaunchBrowser(
                WebAutomationEnums.BrowserEngine.Edge,
                OptionalTemplate(action.Url, vars),
                action.WaitMs,
                action.ShowOnScreen,
                action.StartMaximized),
            WebAutomationActionTypes.AttachChrome => AttachToRunning(
                WebAutomationEnums.BrowserEngine.Chrome,
                action.AttachMode ?? WebAutomationEnums.AttachMode.ByUrl,
                OptionalTemplate(action.Url, vars),
                OptionalTemplate(action.Text, vars),
                action.DebuggingPort is > 0 ? (int?)action.DebuggingPort.Value : null,
                action.TimeoutMs),
            WebAutomationActionTypes.AttachEdge => AttachToRunning(
                WebAutomationEnums.BrowserEngine.Edge,
                action.AttachMode ?? WebAutomationEnums.AttachMode.ByUrl,
                OptionalTemplate(action.Url, vars),
                OptionalTemplate(action.Text, vars),
                action.DebuggingPort is > 0 ? (int?)action.DebuggingPort.Value : null,
                action.TimeoutMs),
            WebAutomationActionTypes.Click => Click(
                RequireSelector(action, vars),
                action.TimeoutMs,
                action.WaitForNavigation ?? false,
                action.WaitSelectorAfter,
                action.WaitAfterTimeoutMs,
                action.DialogAction),
            WebAutomationActionTypes.ClickDownload => ClickDownload(
                RequireSelector(action, vars),
                ResolvePath(OptionalTemplate(action.Path, vars) ?? "output/download", projectRoot),
                action.TimeoutMs),
            WebAutomationActionTypes.WaitNavigation => WaitNavigation(action.TimeoutMs),
            WebAutomationActionTypes.Fill => Fill(
                RequireSelector(action, vars),
                RunVariables.Interpolate(action.Text ?? "", vars),
                action.TimeoutMs,
                action.PopulateMode),
            WebAutomationActionTypes.Focus => Focus(RequireSelector(action, vars), action.TimeoutMs),
            WebAutomationActionTypes.WaitFor => WaitFor(
                RequireSelector(action, vars),
                action.TimeoutMs,
                action.WaitVisible ?? false),
            WebAutomationActionTypes.WaitContent => WaitContent(
                action.WaitContentMode ?? WebAutomationEnums.WaitContentMode.ContainsText,
                RunVariables.Interpolate(action.Text ?? "", vars),
                OptionalTemplate(action.Selector, vars),
                action.TimeoutMs),
            WebAutomationActionTypes.GetText => GetText(
                RequireSelector(action, vars),
                action.TimeoutMs,
                action.Variable,
                vars),
            WebAutomationActionTypes.GetElementDetails => GetElementDetails(
                RequireSelector(action, vars),
                action.Detail ?? WebAutomationEnums.ElementDetail.Text,
                action.TimeoutMs,
                action.Variable,
                vars),
            WebAutomationActionTypes.GetPageDetails => GetPageDetails(
                action.Detail ?? WebAutomationEnums.PageDetail.Url,
                action.Variable,
                vars),
            WebAutomationActionTypes.Screenshot => Screenshot(
                ResolvePath(OptionalTemplate(action.Path, vars) ?? "output/web-screenshot.png", projectRoot),
                action.TimeoutMs),
            WebAutomationActionTypes.ExtractTable => ExtractTable(
                RequireSelector(action, vars),
                action.TimeoutMs,
                action.Variable,
                vars),
            WebAutomationActionTypes.SetCheckbox => SetCheckbox(
                RequireSelector(action, vars),
                action.Checked ?? true,
                action.TimeoutMs),
            WebAutomationActionTypes.SelectRadio => SelectRadio(RequireSelector(action, vars), action.TimeoutMs),
            WebAutomationActionTypes.SetDropdown => SetDropdown(
                RequireSelector(action, vars),
                action.DropdownMode ?? WebAutomationEnums.DropdownMode.ByText,
                RunVariables.Interpolate(action.DropdownValue ?? "", vars),
                action.TimeoutMs),
            WebAutomationActionTypes.PressButton => PressButton(RequireSelector(action, vars), action.TimeoutMs),
            WebAutomationActionTypes.Upload => UploadFile(
                RequireSelector(action, vars),
                ResolvePath(RequireTemplate(action.Path, vars, "path"), projectRoot),
                action.TimeoutMs),
            WebAutomationActionTypes.Hover => Hover(RequireSelector(action, vars), action.TimeoutMs),
            WebAutomationActionTypes.ExecuteScript => ExecuteScript(
                RunVariables.Interpolate(action.Script ?? "", vars),
                action.TimeoutMs,
                action.Variable,
                vars),
            WebAutomationActionTypes.NewTab => NewTab(OptionalTemplate(action.Url, vars), action.TimeoutMs),
            WebAutomationActionTypes.Close => CloseBrowser(),
            WebAutomationActionTypes.Reload => Reload(action.TimeoutMs),
            WebAutomationActionTypes.Back => HistoryNav(back: true, action.TimeoutMs),
            WebAutomationActionTypes.Forward => HistoryNav(back: false, action.TimeoutMs),
            WebAutomationActionTypes.SwitchPage => SwitchPage(OptionalTemplate(action.Url, vars), action.TimeoutMs),
            _ => throw new NotImplementedException(
                $"Web automation `{WebAutomationActionTypes.ModuleActionId(action.Type)}` não implementada."),
        };
    }

    static string RequireTemplate(string? value, RunVariables vars, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"browser action missing `{field}`");
        return RunVariables.Interpolate(value, vars);
    }

    static string? OptionalTemplate(string? value, RunVariables vars) =>
        string.IsNullOrWhiteSpace(value) ? null : RunVariables.Interpolate(value, vars);

    static string RequireSelector(WebAutomationAction action, RunVariables vars)
    {
        if (string.IsNullOrWhiteSpace(action.Selector))
            throw new InvalidOperationException($"browser action `{action.Type}` missing `selector`");
        return RunVariables.Interpolate(action.Selector, vars);
    }

    static string ResolvePath(string path, string? projectRoot)
    {
        if (Path.IsPathRooted(path))
            return path;
        return projectRoot is null ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(projectRoot, path));
    }

    public string AttachToRunning(
        string engine,
        string attachMode,
        string? urlFragment,
        string? titleFragment,
        int? debuggingPort,
        ulong? timeoutMs)
    {
        _preferredEngine = engine;
        EndSession(closeBrowserWindow: _ownsBrowser);

        var (browser, page, port) = ChromiumAttach.ConnectAndPickPageAsync(
                engine,
                attachMode,
                urlFragment,
                titleFragment,
                debuggingPort)
            .GetAwaiter().GetResult();

        _browser = browser;
        _page = page;
        _ownsBrowser = false;

        if (timeoutMs is > 0)
            Thread.Sleep((int)timeoutMs.Value);

        var title = _page.GetTitleAsync().GetAwaiter().GetResult();
        return $"browser_attach: {engine} port={port} mode={attachMode} (title=\"{title}\")";
    }

    public string LaunchBrowser(string engine, string? url, ulong? waitMs, bool? showOnScreen, bool? startMaximized)
    {
        _preferredEngine = engine;
        if (_page is not null)
        {
            if (_ownsBrowser)
                throw new InvalidOperationException("browser session already open; use browser_go_to or browser_close first");
            EndSession(closeBrowserWindow: false);
        }

        var target = string.IsNullOrWhiteSpace(url) ? "about:blank" : url;
        LaunchSession(target, showOnScreen ?? true, startMaximized ?? true, engine).GetAwaiter().GetResult();
        if (waitMs is > 0)
            Thread.Sleep((int)waitMs.Value);
        var title = _page!.GetTitleAsync().GetAwaiter().GetResult();
        return $"browser_launch: {engine} (title=\"{title}\")";
    }

    public string GoToOnly(string url, ulong? waitMs)
    {
        NavigateExisting(url).GetAwaiter().GetResult();
        if (waitMs is > 0)
            Thread.Sleep((int)waitMs.Value);
        var title = _page!.GetTitleAsync().GetAwaiter().GetResult();
        return $"browser_go_to: {url} (title=\"{title}\")";
    }

    public string Focus(string selector, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        RunWithTransientRetry(() =>
        {
            var handle = WaitForSelectorAsync(RequirePage(), selector, timeout).GetAwaiter().GetResult();
            handle.FocusAsync().GetAwaiter().GetResult();
        });
        return $"browser_focus: {selector}";
    }

    public string Fill(string selector, string text, ulong? timeoutMs, string? populateMode)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var append = string.Equals(populateMode, WebAutomationEnums.PopulateMode.Append, StringComparison.OrdinalIgnoreCase);
        var page = RequirePage();
        RunWithTransientRetry(() =>
        {
            var handle = WaitForSelectorAsync(page, selector, timeout).GetAwaiter().GetResult();
            handle.ClickAsync().GetAwaiter().GetResult();
            if (!append)
            {
                handle.EvaluateFunctionAsync(
                    @"el => {
                if (el.value !== undefined) el.value = '';
                else if (el.isContentEditable) el.textContent = '';
            }").GetAwaiter().GetResult();
            }
            handle.TypeAsync(text).GetAwaiter().GetResult();
        });
        var mode = append ? "append" : "replace";
        return $"browser_fill: {selector} ({text.Length} chars, {mode})";
    }

    public string WaitFor(string selector, ulong? timeoutMs, bool visible)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        RunWithTransientRetry(() =>
            WaitForSelectorAsync(RequirePage(), selector, timeout, visible).GetAwaiter().GetResult());
        return $"browser_wait_for: {selector}{(visible ? " (visible)" : "")}";
    }

    public string WaitContent(string mode, string text, string? selector, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(1, timeout));
        var page = RequirePage();
        while (DateTime.UtcNow < deadline)
        {
            if (EvaluateWaitContent(page, mode, text, selector))
                return $"browser_wait_content: {mode} ok";
            Thread.Sleep(150);
        }
        throw new TimeoutException($"timeout ({timeout}ms) waiting for page content `{mode}`");
    }

    static bool EvaluateWaitContent(IPage page, string mode, string text, string? selector)
    {
        try
        {
            return mode switch
            {
                WebAutomationEnums.WaitContentMode.ContainsText =>
                    page.EvaluateFunctionAsync<bool>(
                        "(t) => document.body && (document.body.innerText || '').includes(t)", text)
                        .GetAwaiter().GetResult(),
                WebAutomationEnums.WaitContentMode.NotContainsText =>
                    !page.EvaluateFunctionAsync<bool>(
                        "(t) => document.body && (document.body.innerText || '').includes(t)", text)
                        .GetAwaiter().GetResult(),
                WebAutomationEnums.WaitContentMode.ContainsElement when !string.IsNullOrWhiteSpace(selector) =>
                    page.QuerySelectorAsync(selector).GetAwaiter().GetResult() is not null,
                WebAutomationEnums.WaitContentMode.NotContainsElement when !string.IsNullOrWhiteSpace(selector) =>
                    page.QuerySelectorAsync(selector).GetAwaiter().GetResult() is null,
                _ => throw new InvalidOperationException($"unknown wait_content_mode `{mode}`"),
            };
        }
        catch (Exception ex) when (IsTransientBrowserError(ex))
        {
            // página navegando/reavaliando: a condição ainda não está satisfeita; o poll tenta de novo.
            return false;
        }
    }

    public string GetPageDetails(string detail, string? variable, RunVariables vars)
    {
        var page = RequirePage();
        var url = page.Url;
        var title = page.GetTitleAsync().GetAwaiter().GetResult();
        var value = detail switch
        {
            WebAutomationEnums.PageDetail.Url => url,
            WebAutomationEnums.PageDetail.Title => title,
            WebAutomationEnums.PageDetail.All => JsonSerializer.Serialize(new { url, title }),
            _ => throw new InvalidOperationException($"unknown page detail `{detail}`"),
        };
        StoreVariable(variable, value, vars);
        return $"browser_get_page_details: {detail} -> {Truncate(value, 80)}";
    }

    public string GetElementDetails(string selector, string detail, ulong? timeoutMs, string? variable, RunVariables vars)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var value = RunWithTransientRetry(() =>
        {
            var handle = WaitForSelectorAsync(RequirePage(), selector, timeout).GetAwaiter().GetResult();
            return handle.EvaluateFunctionAsync<string>(
                @"(el, kind) => {
                if (!el) return '';
                switch (kind) {
                  case 'value': return el.value ?? '';
                  case 'href': return el.href ?? '';
                  case 'inner_html': return el.innerHTML ?? '';
                  case 'outer_html': return el.outerHTML ?? '';
                  case 'checked': return String(!!el.checked);
                  case 'tag_name': return (el.tagName || '').toLowerCase();
                  default:
                    return ((el.innerText || el.textContent || '') + '').trim();
                }
            }", detail).GetAwaiter().GetResult() ?? "";
        });
        StoreVariable(variable, value, vars);
        return $"browser_get_element_details: {selector} ({detail}) -> {Truncate(value, 80)}";
    }

    public string Screenshot(string path, ulong? timeoutMs)
    {
        _ = timeoutMs;
        var page = RequirePage();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var bytes = page.ScreenshotDataAsync(new ScreenshotOptions { FullPage = true }).GetAwaiter().GetResult();
        File.WriteAllBytes(path, bytes);
        return $"browser_screenshot: {path}";
    }

    public string ExtractTable(string selector, ulong? timeoutMs, string? variable, RunVariables vars)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var json = RunWithTransientRetry(() =>
        {
            var handle = WaitForSelectorAsync(RequirePage(), selector, timeout).GetAwaiter().GetResult();
            return handle.EvaluateFunctionAsync<string>(
                @"table => {
                const rows = [];
                for (const tr of table.querySelectorAll('tr')) {
                  const cells = [];
                  for (const cell of tr.querySelectorAll('th,td')) cells.push((cell.innerText || '').trim());
                  if (cells.length) rows.push(cells);
                }
                return JSON.stringify(rows);
            }").GetAwaiter().GetResult() ?? "[]";
        });
        var normalized = FlowTable.NormalizeStoredTableJson(json, firstRowIsHeader: true);
        StoreVariable(variable, normalized, vars);
        return $"browser_extract_table: {selector} ({normalized.Length} chars json)";
    }

    public string SetCheckbox(string selector, bool check, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        RunWithTransientRetry(() =>
        {
            var handle = WaitForSelectorAsync(RequirePage(), selector, timeout).GetAwaiter().GetResult();
            handle.EvaluateFunctionAsync(
                @"(el, desired) => {
                if (!el) return;
                if (!!el.checked !== desired) el.click();
            }", check).GetAwaiter().GetResult();
        });
        return $"browser_set_checkbox: {selector} -> {check}";
    }

    public string SelectRadio(string selector, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        RunWithTransientRetry(() =>
        {
            var handle = WaitForSelectorAsync(RequirePage(), selector, timeout).GetAwaiter().GetResult();
            handle.ClickAsync().GetAwaiter().GetResult();
        });
        return $"browser_select_radio: {selector}";
    }

    /// <summary>
    /// Envia um arquivo local para um <c>input[type=file]</c> via CDP (DOM.setFileInputFiles).
    /// Funciona com uploaders JS (FineUploader/qq-uploader, Dropzone, …) porque o navegador dispara
    /// <c>input</c>/<c>change</c> como se o usuário tivesse escolhido o arquivo.
    /// </summary>
    public string UploadFile(string selector, string path, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
            throw new FileNotFoundException($"browser_upload: arquivo nao encontrado `{full}`");

        RunWithTransientRetry(() =>
        {
            var handle = WaitForSelectorAsync(RequirePage(), selector, timeout).GetAwaiter().GetResult();
            handle.UploadFileAsync(full).GetAwaiter().GetResult();
        });
        return $"browser_upload: {selector} <- {full}";
    }

    public string SetDropdown(string selector, string mode, string value, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        RunWithTransientRetry(() =>
        {
            var handle = WaitForSelectorAsync(RequirePage(), selector, timeout).GetAwaiter().GetResult();
            handle.EvaluateFunctionAsync(
                @"(el, mode, val) => {
                if (!el || el.tagName !== 'SELECT') throw new Error('element is not SELECT');
                if (mode === 'by_index') {
                  const i = parseInt(val, 10);
                  if (Number.isNaN(i) || i < 0 || i >= el.options.length) throw new Error('index out of range');
                  el.selectedIndex = i;
                } else if (mode === 'by_value') {
                  el.value = val;
                } else {
                  let found = false;
                  for (const opt of el.options) {
                    if ((opt.text || '').trim() === val) { el.value = opt.value; found = true; break; }
                  }
                  if (!found) throw new Error('option not found: ' + val);
                }
                el.dispatchEvent(new Event('change', { bubbles: true }));
            }", mode, value).GetAwaiter().GetResult();
        });
        return $"browser_set_dropdown: {selector} ({mode}={value})";
    }

    public string PressButton(string selector, ulong? timeoutMs) => Click(selector, timeoutMs, false, null, null, null);

    /// <summary>
    /// Troca a página ativa para outra janela/aba do mesmo navegador (popup aberto por
    /// <c>window.open</c>). Casa por trecho de URL; sem <paramref name="urlMatch"/> pega a
    /// primeira página diferente da atual. Use de novo para voltar à página original.
    /// </summary>
    public string SwitchPage(string? urlMatch, ulong? timeoutMs)
    {
        var browser = _browser ?? RequirePage().Browser;
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        var vistas = new List<string>();
        while (DateTime.UtcNow < deadline)
        {
            var pages = browser.PagesAsync().GetAwaiter().GetResult();
            foreach (var page in pages)
            {
                if (ReferenceEquals(page, _page))
                    continue;
                string url;
                try { url = page.Url ?? ""; }
                catch { continue; } // alvo fechado no meio do caminho
                if (url.Length == 0 || url == "about:blank")
                    continue;
                if (!vistas.Contains(url))
                    vistas.Add(url);
                if (urlMatch is not null && !url.Contains(urlMatch, StringComparison.OrdinalIgnoreCase))
                    continue;
                _page = page;
                return $"browser_switch_page: {url}";
            }
            Thread.Sleep(200);
        }
        var encontradas = vistas.Count == 0 ? "nenhuma outra pagina" : string.Join(" | ", vistas);
        throw new TimeoutException($"browser_switch_page: nenhuma pagina casando `{urlMatch}` em {timeout}ms ({encontradas})");
    }

    public string Hover(string selector, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        RunWithTransientRetry(() =>
        {
            var handle = WaitForSelectorAsync(RequirePage(), selector, timeout).GetAwaiter().GetResult();
            handle.HoverAsync().GetAwaiter().GetResult();
        });
        return $"browser_hover: {selector}";
    }

    public string ExecuteScript(string script, ulong? timeoutMs, string? variable, RunVariables vars)
    {
        _ = timeoutMs;
        var page = RequirePage();
        var result = RunWithTransientRetry(() =>
            page.EvaluateExpressionAsync<object?>(script).GetAwaiter().GetResult());
        var text = result switch
        {
            null => "",
            string s => s,
            _ => JsonSerializer.Serialize(result),
        };
        StoreVariable(variable, text, vars);
        return $"browser_execute_script: -> {Truncate(text, 80)}";
    }

    public string NewTab(string? url, ulong? timeoutMs)
    {
        var page = RequirePage();
        _page = page.Browser.NewPageAsync().GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(url))
            GoToCompatAsync(_page, url).GetAwaiter().GetResult();
        if (timeoutMs is > 0)
            Thread.Sleep((int)timeoutMs.Value);
        return $"browser_new_tab: {url ?? "about:blank"}";
    }

    public string CloseBrowser()
    {
        var owned = _ownsBrowser;
        EndSession(closeBrowserWindow: owned);
        return owned ? "browser_close: browser closed" : "browser_close: detached (browser left open)";
    }

    public string Reload(ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var page = RequirePage();
        RunWithTransientRetry(() =>
        {
            page.ReloadAsync(Math.Max(1, timeout)).GetAwaiter().GetResult();
            WaitForDomStableAsync(page, Math.Min(timeout, 8000)).GetAwaiter().GetResult();
        });
        return "browser_reload: ok";
    }

    public string HistoryNav(bool back, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var page = RequirePage();
        RunWithTransientRetry(() =>
        {
            var task = back ? page.GoBackAsync() : page.GoForwardAsync();
            task.WaitAsync(TimeSpan.FromMilliseconds(Math.Max(1, timeout))).GetAwaiter().GetResult();
            WaitForDomStableAsync(page, Math.Min(timeout, 8000)).GetAwaiter().GetResult();
        });
        return back ? "browser_back: ok" : "browser_forward: ok";
    }

    public string ClickDownload(string selector, string path, ulong? timeoutMs)
    {
        var timeout = (int)(timeoutMs ?? DefaultTimeoutMs);
        var page = RequirePage();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        RunWithTransientRetry(() =>
        {
            var client = page.Client;
            client.SendAsync("Page.setDownloadBehavior", new { behavior = "allow", downloadPath = dir ?? "." })
                .GetAwaiter().GetResult();
            WaitForSelectorAsync(page, selector, timeout).GetAwaiter().GetResult().ClickAsync().GetAwaiter().GetResult();
            var deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(1, timeout));
            while (DateTime.UtcNow < deadline)
            {
                if (Directory.GetFiles(dir ?? ".", "*", SearchOption.TopDirectoryOnly)
                    .Any(f => f.EndsWith(".crdownload", StringComparison.OrdinalIgnoreCase) == false
                              && File.GetLastWriteTimeUtc(f) > DateTime.UtcNow.AddSeconds(-30)))
                    return;
                Thread.Sleep(200);
            }
            throw new TimeoutException($"timeout ({timeout}ms) waiting for download after click");
        });
        return $"browser_click_download: {selector} -> {path}";
    }

    static void StoreVariable(string? variable, string value, RunVariables vars)
    {
        if (!string.IsNullOrWhiteSpace(variable))
            vars[variable.Trim()] = value;
    }

    static string Truncate(string text, int max) =>
        text.Length > max ? text[..max] + "…" : text;
}
