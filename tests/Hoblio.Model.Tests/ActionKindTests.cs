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
    public void Show_select_from_list_roundtrip_and_produced()
    {
        var action = new ShowSelectFromListAction
        {
            Title = "Escolher",
            Message = "Pick one",
            List = "A\nB",
            SelectedItemVariable = "SelectedItem",
            SelectedIndexVariable = "SelectedIndex",
            ButtonPressedVariable = "ButtonPressed",
        };
        var json = JsonSerializer.Serialize(action, HoblioJson.Options);
        Assert.Contains("show_select_from_list", json);
        var back = JsonSerializer.Deserialize<ActionKind>(json, HoblioJson.Options);
        Roundtrip(action);
        Assert.Equal(["SelectedItem", "SelectedIndex", "ButtonPressed"], back!.ProducedVariables().ToArray());
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

    [Fact]
    public void Http_get_optional_variable_roundtrip()
    {
        Roundtrip(new HttpGetAction { Url = "https://example.com" });
        var withVar = new HttpGetAction { Url = "https://example.com", Variable = "body" };
        Roundtrip(withVar);
        Assert.Equal(["body"], withVar.ProducedVariables().ToArray());
    }

    [Fact]
    public void Excel_actions_roundtrip_and_catalog_id()
    {
        var launch = new ExcelAction
        {
            Operation = ExcelActionTypes.Launch,
            Instance = "ExcelInstance1",
            Path = "dados.xlsx",
            Visible = true,
        };
        Roundtrip(launch);
        var read = new ExcelAction
        {
            Operation = ExcelActionTypes.Read,
            Instance = "ExcelInstance1",
            StartRow = "2",
            StartColumn = "B",
            OutputVariable = "Valor",
        };
        Roundtrip(read);
        Assert.Equal(["Valor"], read.ProducedVariables().ToArray());
        var desc = ActionCatalog.Describe(read);
        Assert.Equal("Excel", desc.Module);
        Assert.Equal("ReadFromExcel", desc.ActionId);
    }

    [Fact]
    public void Word_actions_roundtrip_and_catalog_id()
    {
        var launch = new WordAction
        {
            Operation = WordActionTypes.Launch,
            Instance = "WordInstance1",
            Visible = true,
        };
        Roundtrip(launch);
        var read = new WordAction
        {
            Operation = WordActionTypes.Read,
            Instance = "WordInstance1",
            Mode = WordEnums.ReadMode.Pages,
            Pages = "1-2",
            OutputVariable = "WordData",
        };
        Roundtrip(read);
        var desc = ActionCatalog.Describe(read);
        Assert.Equal("Word", desc.Module);
        Assert.Equal("ReadFromWord", desc.ActionId);
    }

    [Fact]
    public void Excel_extended_fields_roundtrip_json()
    {
        var filter = new ExcelAction
        {
            Operation = ExcelActionTypes.FilterCells,
            Instance = "ExcelInstance1",
            AdvancedFilter = true,
            FilterOperator1 = ExcelEnums.FilterOperator.GreaterThan,
            FilterValue1 = "10",
            FilterOperator2 = ExcelEnums.FilterOperator.LessThan,
            FilterValue2 = "99",
            FilterLogic = ExcelEnums.FilterLogic.And,
        };
        Roundtrip(filter);

        var copy = new ExcelAction
        {
            Operation = ExcelActionTypes.CopyWorksheet,
            Instance = "ExcelA",
            TargetInstance = "ExcelB",
            SourceWorksheetName = "Dados",
            TargetWorksheetName = "Dados_copia",
            WorksheetPosition = ExcelEnums.WorksheetPosition.After,
            PasteMode = ExcelEnums.PasteMode.Values,
        };
        Roundtrip(copy);
    }
}
