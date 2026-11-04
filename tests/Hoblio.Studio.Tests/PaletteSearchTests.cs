using Hoblio.Studio;
using Xunit;

namespace Hoblio.Studio.Tests;

public sealed class PaletteSearchTests
{
    [Fact]
    public void Matches_finds_english_alias()
    {
        var index = PaletteSearch.BuildSearchIndex(
            "action:http_get",
            "HTTP GET",
            "Requisição HTTP GET",
            "HttpGet http rede web");
        Assert.True(PaletteSearch.Matches(index, "HttpGet"));
        Assert.True(PaletteSearch.Matches(index, "rede"));
        Assert.False(PaletteSearch.Matches(index, "xyz"));
    }

    [Fact]
    public void Matches_empty_query_matches_all()
    {
        var index = PaletteSearch.BuildSearchIndex("action:log", "Log", null, null);
        Assert.True(PaletteSearch.Matches(index, ""));
        Assert.True(PaletteSearch.Matches(index, "   "));
    }
}
