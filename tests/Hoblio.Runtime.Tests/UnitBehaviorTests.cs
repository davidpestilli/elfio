using Hoblio.Model;
using Hoblio.Runtime;

namespace Hoblio.Runtime.Tests;

public class UnitBehaviorTests
{
    [Fact]
    public void Interpolate_replaces_known_vars()
    {
        var vars = new RunVariables { ["page_heading"] = "Hello" };
        Assert.Equal("Title: Hello", RunVariables.Interpolate("Title: {page_heading}", vars));
    }

    [Fact]
    public void Interpolate_errors_on_missing_var()
    {
        Assert.Throws<InvalidOperationException>(() => RunVariables.Interpolate("{missing}", new RunVariables()));
    }

    [Fact]
    public void Eval_if_lt_numeric()
    {
        var vars = new RunVariables { ["counter"] = "3" };
        var cfg = new IfControl("{counter}", CompareOperator.Lt, "4", false);
        Assert.True(ExpressionEvaluator.EvalIf(cfg, vars));
    }

    [Fact]
    public void Parse_json_list()
    {
        Assert.Equal(["a", "b"], ExpressionEvaluator.ParseStringList("""["a","b"]"""));
    }

    [Fact]
    public void Increase_from_missing_treats_as_zero()
    {
        var vars = new RunVariables();
        AdjustVariable.ExecuteAdjust(new IncreaseVariableAction { Variable = "counter" }, vars, 1);
        Assert.Equal("1", vars["counter"]);
    }

    [Fact]
    public void Increase_rejects_non_numeric_with_context()
    {
        var vars = new RunVariables { ["v"] = "abc" };
        var ex = Assert.Throws<HoblioRunException>(() =>
            AdjustVariable.ExecuteAdjust(new IncreaseVariableAction { Variable = "v" }, vars, 1));
        Assert.Contains("must be numeric", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void List_append_creates_list()
    {
        var vars = new RunVariables();
        ListActions.ListAppend(vars, "items", "alpha");
        ListActions.ListAppend(vars, "items", "beta");
        Assert.Equal(["alpha", "beta"], ExpressionEvaluator.ParseStringList(vars["items"]));
    }

    [Fact]
    public void Run_event_json_not_empty_object()
    {
        var json = RunEventJson.Serialize(new RunEvent.ActionExecuted("n1", "log: hi", null));
        Assert.Contains("action_executed", json);
        Assert.Contains("log: hi", json);
        Assert.DoesNotContain("{}", json);
    }

    [Fact]
    public void Eval_if_eq_string()
    {
        var vars = new RunVariables { ["flag"] = "yes" };
        var cfg = new IfControl("flag", CompareOperator.Eq, "yes", false);
        Assert.True(ExpressionEvaluator.EvalIf(cfg, vars));
    }

    [Fact]
    public void List_clear_empties_list()
    {
        var vars = new RunVariables { ["items"] = """["a","b"]""" };
        ListActions.ListClear(vars, "items");
        Assert.Equal("[]", vars["items"]);
    }

    [Fact]
    public void Decrease_variable()
    {
        var vars = new RunVariables { ["counter"] = "3" };
        AdjustVariable.ExecuteAdjust(new DecreaseVariableAction { Variable = "counter" }, vars, -1);
        Assert.Equal("2", vars["counter"]);
    }

    [Fact]
    public void Format_chain_matches_anyhow_style()
    {
        var inner = new InvalidOperationException("expected a number, got `abc`");
        var outer = new HoblioRunException("variable `v` must be numeric for increase/decrease", inner);
        var text = RunErrors.FormatChain(outer);
        Assert.Contains("Caused by:", text);
        Assert.Contains("expected a number", text);
    }
}
