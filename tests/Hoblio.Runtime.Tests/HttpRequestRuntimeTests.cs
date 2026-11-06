using System.Net;
using System.Text;
using Hoblio.Model;
using Hoblio.Runtime;

namespace Hoblio.Runtime.Tests;

public sealed class HttpRequestRuntimeTests
{
    [Fact]
    public void Http_request_get_stores_body_and_status()
    {
        using var srv = new LoopbackHttpListener(async ctx =>
        {
            Assert.Equal("GET", ctx.Request.HttpMethod);
            var bytes = Encoding.UTF8.GetBytes("""{"ok":true}""");
            ctx.Response.ContentType = "application/json";
            await ctx.Response.OutputStream.WriteAsync(bytes);
        });

        var vars = new RunVariables();
        using var browser = new BrowserContext();
        using var excel = new ExcelContext();
        using var word = new WordContext();
        using var pdf = new PdfContext();
        var action = new HttpRequestAction
        {
            Url = $"{srv.BaseUrl}items",
            Method = "get",
            Accept = "application/json",
            ResponseBodyVariable = "Body",
            StatusCodeVariable = "Code",
        };

        var summary = ActionExecutor.Execute(action, browser, excel, word, pdf, vars, null, CancellationToken.None);
        Assert.Contains("http_request GET", summary);
        Assert.Equal("""{"ok":true}""", vars["Body"]);
        Assert.Equal("200", vars["Code"]);
    }

    [Fact]
    public void Http_request_post_sends_body_and_custom_headers()
    {
        using var srv = new LoopbackHttpListener(async ctx =>
        {
            Assert.Equal("POST", ctx.Request.HttpMethod);
            Assert.Equal("1", ctx.Request.Headers["X-Test"]);
            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            Assert.Contains("a", body, StringComparison.Ordinal);
            var bytes = Encoding.UTF8.GetBytes("created");
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.OutputStream.WriteAsync(bytes);
        });

        var vars = new RunVariables { ["payload"] = """{"name":"a"}""" };
        using var browser = new BrowserContext();
        using var excel = new ExcelContext();
        using var word = new WordContext();
        using var pdf = new PdfContext();
        var action = new HttpRequestAction
        {
            Url = srv.BaseUrl,
            Method = "post",
            ContentType = "application/json",
            Body = "{payload}",
            CustomHeaders = "X-Test: 1",
            ResponseBodyVariable = "R",
        };

        ActionExecutor.Execute(action, browser, excel, word, pdf, vars, null, CancellationToken.None);
        Assert.Equal("created", vars["R"]);
    }

    sealed class LoopbackHttpListener : IDisposable
    {
        readonly HttpListener _listener = new();
        readonly CancellationTokenSource _cts = new();
        readonly Task _loop;

        public string BaseUrl { get; }

        public LoopbackHttpListener(Func<HttpListenerContext, Task> handle)
        {
            var prefix = $"http://127.0.0.1:{GetFreeTcpPort()}/";
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            BaseUrl = prefix;
            _loop = Task.Run(() => Run(handle, _cts.Token));
        }

        static int GetFreeTcpPort()
        {
            var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        async Task Run(Func<HttpListenerContext, Task> handle, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (HttpListenerException)
                {
                    break;
                }

                try
                {
                    await handle(ctx).ConfigureAwait(false);
                }
                finally
                {
                    try { ctx.Response.OutputStream.Close(); }
                    catch { /* ignore */ }
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _listener.Stop(); } catch { /* ignore */ }
            try { _listener.Close(); } catch { /* ignore */ }
            _cts.Dispose();
            try { _loop.GetAwaiter().GetResult(); }
            catch { /* ignore */ }
        }
    }
}
