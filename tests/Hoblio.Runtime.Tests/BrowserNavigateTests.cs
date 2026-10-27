using Hoblio.Runtime;

namespace Hoblio.Runtime.Tests;

public class BrowserNavigateTests
{
    [Fact]
    public void Browser_dom_actions_require_navigate_first()
    {
        using var ctx = new BrowserContext();
        var ex = Assert.Throws<InvalidOperationException>(() => ctx.Click("body", null, false, null, null));
        Assert.Contains("browser_navigate", ex.Message);
    }

    [Fact]
    public void Browser_navigate_local_html_when_chrome_available()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("HOBLIO_BROWSER_TEST"), "1", StringComparison.Ordinal))
        {
            return; // skip unless explicitly enabled (CI/dev with Chrome)
        }

        var html = Path.Combine(Path.GetTempPath(), $"hoblio-nav-test-{Guid.NewGuid():N}.html");
        File.WriteAllText(html, "<html><head><title>HoblioTest</title></head><body><p>ok</p></body></html>");
        try
        {
            using var ctx = new BrowserContext();
            var summary = ctx.Navigate(new Uri(html).AbsoluteUri, null, false, false);
            Assert.Contains("HoblioTest", summary);
            ctx.WaitFor("p", 5000);
            var vars = new RunVariables();
            var text = ctx.GetText("p", 5000, null, vars);
            Assert.Contains("ok", text);
        }
        finally
        {
            File.Delete(html);
        }
    }
}
