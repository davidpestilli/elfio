using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using Hoblio.Studio.ViewModels;
using Microsoft.Web.WebView2.Core;

namespace Hoblio.Studio.FlowCanvas;

public partial class FlowCanvasHost : UserControl
{
    const string VirtualHost = "hoblio.canvas";
    FlowEditorViewModel? _vm;
    bool _webReady;

    public FlowCanvasHost()
    {
        InitializeComponent();
        Loaded += OnLoadedAsync;
    }

    public void AttachViewModel(FlowEditorViewModel vm)
    {
        _vm = vm;
        vm.FlowCanvas = this;
        if (_webReady)
            SyncFromViewModel();
    }

    async void OnLoadedAsync(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            await WebView.EnsureCoreWebView2Async();
            var folder = ResolveCanvasFolder();
            if (!Directory.Exists(folder))
            {
                WebView.NavigateToString(
                    "<html><body style='font-family:Segoe UI;padding:24px'>" +
                    "<h3>Flow canvas não encontrado</h3>" +
                    "<p>Execute <code>npm run build</code> em <code>src/Hoblio.Studio/flow-canvas</code>.</p>" +
                    "</body></html>");
                return;
            }

            WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                VirtualHost,
                folder,
                CoreWebView2HostResourceAccessKind.Allow);
            WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            WebView.Source = new Uri($"https://{VirtualHost}/index.html");
        }
        catch (Exception ex)
        {
            WebView.NavigateToString(
                $"<html><body style='font-family:Segoe UI;padding:24px'><h3>WebView2</h3><pre>{ex.Message}</pre></body></html>");
        }
    }

    static string ResolveCanvasFolder()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "flow-canvas"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "flow-canvas", "dist")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "flow-canvas", "dist")),
        };
        foreach (var path in candidates)
        {
            if (File.Exists(Path.Combine(path, "index.html")))
                return path;
        }
        return candidates[0];
    }

    void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.TryGetWebMessageAsString();
            if (string.IsNullOrWhiteSpace(json) || _vm is null)
                return;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeEl))
                return;
            var type = typeEl.GetString();
            switch (type)
            {
                case "ready":
                    _webReady = true;
                    SyncFromViewModel();
                    break;
                case "nodesMoved":
                    _vm.CanvasApplyNodesMoved(root.GetProperty("payload"));
                    break;
                case "connect":
                    _vm.CanvasApplyConnect(root.GetProperty("payload"));
                    break;
                case "connectEnd":
                    _vm.CanvasApplyConnectEnd(root.GetProperty("payload"));
                    break;
                case "selection":
                    _vm.CanvasApplySelection(root.GetProperty("payload"));
                    break;
                case "removeEdge":
                    _vm.CanvasApplyRemoveEdge(root.GetProperty("payload"));
                    break;
                case "waypoints":
                    _vm.CanvasApplyWaypoints(root.GetProperty("payload"));
                    break;
                case "shortcut":
                    _vm.CanvasApplyShortcut(root.GetProperty("payload"));
                    break;
                case "reconnect":
                    _vm.CanvasApplyReconnect(root.GetProperty("payload"));
                    break;
                case "deleteNodes":
                    _vm.CanvasDeleteNodes(root.GetProperty("payload"));
                    break;
                case "toolbar":
                    _vm.CanvasApplyToolbar(root.GetProperty("payload"));
                    break;
            }
        }
        catch
        {
            /* bridge errors should not crash the host */
        }
    }

    public void SyncFromViewModel()
    {
        if (!_webReady || _vm is null || WebView.CoreWebView2 is null)
            return;
        var message = FlowCanvasSerializer.BuildSetGraph(_vm);
        PostJson(message);
    }

    public void PatchEdgeWaypoints(string edgeId, IReadOnlyList<(double X, double Y)> waypoints)
    {
        if (!_webReady || WebView.CoreWebView2 is null)
            return;
        PostJson(new
        {
            type = "patchEdge",
            payload = new
            {
                id = edgeId,
                waypoints = waypoints.Select(w => new { x = w.X, y = w.Y }).ToArray(),
            },
        });
    }

    public void PatchToolbarState(bool canUndo, bool canRedo)
    {
        if (!_webReady || WebView.CoreWebView2 is null)
            return;
        PostJson(new
        {
            type = "patchToolbar",
            payload = new { canUndo, canRedo },
        });
    }

    public void PatchNodePresentation(IEnumerable<FlowNodeViewModel> nodes)
    {
        if (!_webReady || WebView.CoreWebView2 is null)
            return;
        var payload = nodes.Select(n => new
        {
            id = n.Id,
            label = n.Label,
            summary = n.Summary,
            isActive = n.IsActive,
        }).ToArray();
        PostJson(new { type = "patchNodes", payload = new { nodes = payload } });
    }

    void PostJson(object message)
    {
        if (WebView.CoreWebView2 is null)
            return;
        var json = JsonSerializer.Serialize(message, FlowCanvasSerializer.Options);
        WebView.CoreWebView2.PostWebMessageAsString(json);
    }
}
