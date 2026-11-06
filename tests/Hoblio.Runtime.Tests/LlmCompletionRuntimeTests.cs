using System.Net;
using System.Text;
using Hoblio.Model;
using Hoblio.Runtime;

namespace Hoblio.Runtime.Tests;

public sealed class LlmCompletionRuntimeTests
{
    [Fact]
    public void Llm_completion_ollama_style_posts_expected_json()
    {
        using var srv = new LoopbackHttpListener(async ctx =>
        {
            Assert.Equal("POST", ctx.Request.HttpMethod);
            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            Assert.Contains("\"model\":\"m1\"", body, StringComparison.Ordinal);
            Assert.Contains("\"role\":\"user\"", body, StringComparison.Ordinal);
            Assert.Contains("hello", body, StringComparison.Ordinal);
            await ctx.Response.OutputStream.WriteAsync("ok"u8.ToArray());
        });

        var vars = new RunVariables();
        using var browser = new BrowserContext();
        using var excel = new ExcelContext();
        using var word = new WordContext();
        using var pdf = new PdfContext();
        var action = new LlmCompletionAction
        {
            Url = srv.BaseUrl,
            ApiStyle = "ollama_chat",
            Model = "m1",
            User = "hello",
            ResponseBodyVariable = "R",
        };

        var summary = ActionExecutor.Execute(action, browser, excel, word, pdf, vars, null, CancellationToken.None);
        Assert.Contains("llm_completion", summary);
        Assert.Equal("ok", vars["R"]);
    }

    [Fact]
    public void Llm_completion_openai_style_posts_expected_json()
    {
        using var srv = new LoopbackHttpListener(async ctx =>
        {
            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            Assert.Contains("\"response_format\"", body, StringComparison.Ordinal);
            Assert.Contains("json_object", body, StringComparison.Ordinal);
            await ctx.Response.OutputStream.WriteAsync("{}"u8.ToArray());
        });

        var vars = new RunVariables();
        using var browser = new BrowserContext();
        using var excel = new ExcelContext();
        using var word = new WordContext();
        using var pdf = new PdfContext();
        var action = new LlmCompletionAction
        {
            Url = srv.BaseUrl,
            ApiStyle = "openai_chat",
            Model = "gpt-test",
            System = "sys",
            User = "u",
            JsonSchema = "json_object",
            ResponseBodyVariable = "Out",
        };

        ActionExecutor.Execute(action, browser, excel, word, pdf, vars, null, CancellationToken.None);
        Assert.Equal("{}", vars["Out"]);
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
