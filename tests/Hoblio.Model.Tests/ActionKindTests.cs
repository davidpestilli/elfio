using System.Text.Json;
using Hoblio.Model;
using Hoblio.Model.Json;

namespace Hoblio.Model.Tests;

public class ActionKindTests
{
    static void Roundtrip(ActionKind action)
    {
        var json = JsonSerializer.Serialize(action, HoblioJson.Options);
        var back = JsonSerializer.Deserialize<ActionKind>(json, HoblioJson.Options);
        Assert.NotNull(back);
        var json2 = JsonSerializer.Serialize(back, HoblioJson.Options);
        Assert.Equal(json, json2);
    }

    [Fact]
    public void Browser_actions_roundtrip_json()
    {
        Roundtrip(new BrowserClickAction
        {
            Selector = "#go",
            TimeoutMs = 5000,
            WaitForNavigation = true,
        });
        Roundtrip(new BrowserWaitNavigationAction { TimeoutMs = 8000 });
        Roundtrip(new BrowserFillAction { Selector = "input[name=q]", Text = "hello" });
        Roundtrip(new BrowserWaitForAction { Selector = "h1", TimeoutMs = 3000 });
        Roundtrip(new BrowserGetTextAction { Selector = "h1", Variable = "title" });
    }

    [Fact]
    public void Browser_navigate_show_on_screen_defaults_and_roundtrip()
    {
        var json = """{"type":"browser_navigate","url":"https://example.com"}""";
        var action = JsonSerializer.Deserialize<ActionKind>(json, HoblioJson.Options);
        var nav = Assert.IsType<BrowserNavigateAction>(action);
        Assert.Equal("https://example.com", nav.Url);
        Assert.Null(nav.WaitMs);
        Assert.Null(nav.ShowOnScreen);

        var withFlag = new BrowserNavigateAction
        {
            Url = "https://example.com",
            WaitMs = 100,
            ShowOnScreen = false,
            StartMaximized = false,
        };
        Roundtrip(withFlag);
        var serialized = JsonSerializer.Serialize(withFlag, HoblioJson.Options);
        Assert.Contains("show_on_screen", serialized);
    }

    [Fact]
    public void Browser_get_text_snake_case_tag()
    {
        var json = """{"type":"browser_get_text","selector":"h1"}""";
        var action = JsonSerializer.Deserialize<ActionKind>(json, HoblioJson.Options);
        var getText = Assert.IsType<BrowserGetTextAction>(action);
        Assert.Equal("h1", getText.Selector);
        Assert.Null(getText.TimeoutMs);
        Assert.Null(getText.Variable);
    }

    [Fact]
    public void Show_get_user_input_roundtrip_and_produced()
    {
        var action = new ShowGetUserInputAction
        {
            Title = "Novo",
            Message = "Teste",
            DefaultValue = "",
            UserInputVariable = "UserInput",
            ButtonPressedVariable = "ButtonPressed",
        };
        var json = JsonSerializer.Serialize(action, HoblioJson.Options);
        Assert.Contains("show_get_user_input", json);
        var back = JsonSerializer.Deserialize<ActionKind>(json, HoblioJson.Options);
        Roundtrip(action);
        Assert.Equal(["UserInput", "ButtonPressed"], back!.ProducedVariables().ToArray());
    }

    [Fact]
    public void Set_variable_roundtrip_and_produced()
    {
        var action = new SetVariableAction { Variable = "flag", Value = "{other}" };
        var json = JsonSerializer.Serialize(action, HoblioJson.Options);
        Assert.Contains("set_variable", json);
        Roundtrip(action);
        Assert.Equal(["flag"], action.ProducedVariables().ToArray());
    }
}
