using Hoblio.Model;
using Hoblio.Runtime;

namespace Hoblio.Runtime.Tests;

public sealed class ChromiumPageSelectionTests
{
    static ChromiumPageSelection.Candidate C(string url, string title) => new(url, title);

    [Fact]
    public void Pick_by_url_chooses_shortest_match()
    {
        var pages = new[] { C("https://example.com/long/path", "A"), C("https://example.com", "B") };
        var picked = ChromiumPageSelection.Pick(pages, WebAutomationEnums.AttachMode.ByUrl, "example.com", null, null);
        Assert.Equal("https://example.com", picked!.Url);
    }

    [Fact]
    public void Pick_by_title_requires_fragment()
    {
        var pages = new[] { C("https://a.com", "Login - Portal") };
        var picked = ChromiumPageSelection.Pick(pages, WebAutomationEnums.AttachMode.ByTitle, null, "Portal", null);
        Assert.Equal("Login - Portal", picked!.Title);
    }

    [Fact]
    public void Pick_foreground_matches_normalized_title()
    {
        var pages = new[] { C("https://a.com", "Dashboard"), C("https://b.com", "Other") };
        var picked = ChromiumPageSelection.Pick(
            pages,
            WebAutomationEnums.AttachMode.Foreground,
            null,
            null,
            "Dashboard - Google Chrome");
        Assert.Equal("Dashboard", picked!.Title);
    }

    [Fact]
    public void Skips_extension_urls()
    {
        var pages = new[] { C("chrome-extension://abc/options.html", "Ext"), C("https://real.com", "Real") };
        var picked = ChromiumPageSelection.Pick(pages, WebAutomationEnums.AttachMode.ByUrl, "real.com", null, null);
        Assert.Equal("https://real.com", picked!.Url);
    }
}
