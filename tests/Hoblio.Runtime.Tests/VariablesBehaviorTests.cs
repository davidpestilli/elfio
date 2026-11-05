using Hoblio.Model;
using Hoblio.Runtime;

namespace Hoblio.Runtime.Tests;

public sealed class VariablesBehaviorTests
{
    [Fact]
    public void Datatable_create_add_row_and_column_to_list()
    {
        var vars = new RunVariables();
        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.CreateDatatable,
            DataTableVariable = "T",
            Values = "Name,Score",
        }, vars, null);

        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.AddRow,
            DataTableVariable = "T",
            Values = "Alice,10",
        }, vars, null);

        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.ColumnToList,
            DataTableVariable = "T",
            ColumnName = "Name",
            ListVariable = "Names",
        }, vars, null);

        Assert.Contains("Alice", vars["Names"]);
    }

    [Fact]
    public void List_merge_and_subtract()
    {
        var vars = new RunVariables { ["A"] = "[\"1\",\"2\",\"3\"]", ["B"] = "[\"2\",\"4\"]" };
        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.SubtractLists,
            ListVariable = "A",
            SecondListVariable = "B",
            OutputVariable = "Diff",
        }, vars, null);

        Assert.Equal("[\"1\",\"3\"]", vars["Diff"]);
    }

    [Fact]
    public void Merge_datatables_ignore_extra_columns()
    {
        var vars = new RunVariables
        {
            ["A"] = "{\"columns\":[\"Id\",\"Name\"],\"rows\":[[\"1\",\"a\"]]}",
            ["B"] = "{\"columns\":[\"Id\",\"Name\",\"Extra\"],\"rows\":[[\"2\",\"b\",\"x\"]]}",
        };
        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.MergeDatatables,
            DataTableVariable = "A",
            SecondDataTableVariable = "B",
            Mode = VariablesEnums.MergeMode.IgnoreExtraColumns,
        }, vars, null);
        var merged = FlowTable.Parse(vars["A"]);
        Assert.Equal(2, merged.Rows.Count);
        Assert.Equal(2, merged.Columns.Count);
    }

    [Fact]
    public void Read_csv_fixed_width()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hoblio-vars-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "fixed.txt");
        File.WriteAllText(path, "ID  NAME \r\n01  Alice\r\n02  Bob   \r\n");
        var vars = new RunVariables();
        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.ReadCsv,
            Path = path,
            DataTableVariable = "T",
            Mode = VariablesEnums.CsvReadMode.FixedWidth,
            ColumnWidths = "4,6",
            FirstLineIsHeader = true,
        }, vars, dir);
        var table = FlowTable.Parse(vars["T"]);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Alice", table.Rows[0][1]);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Filter_list_not_contains()
    {
        var vars = new RunVariables { ["L"] = "[\"abc\",\"def\",\"xyz\"]" };
        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.FilterList,
            ListVariable = "L",
            FilterOperator = VariablesEnums.FilterOperator.NotContains,
            FilterValue = "x",
            OutputVariable = "Out",
        }, vars, null);
        Assert.Equal("[\"abc\",\"def\"]", vars["Out"]);
    }

    [Fact]
    public void Json_to_object_does_not_interpolate_braces_in_literal()
    {
        var vars = new RunVariables();
        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.JsonToCustomObject,
            JsonText = "{\"demo\":true}",
            OutputVariable = "Obj",
        }, vars, null);
        Assert.Contains("demo", vars["Obj"]);
    }

    [Fact]
    public void Filter_datatable_three_conditions()
    {
        var vars = new RunVariables
        {
            ["T"] = "{\"columns\":[\"Score\",\"Status\"],\"rows\":[[\"80\",\"ok\"],[\"80\",\"bad\"],[\"50\",\"ok\"]]}",
        };
        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.FilterDatatable,
            DataTableVariable = "T",
            FilterConditions = """
                [
                  {"column":"Score","operator":"gte","value":"70"},
                  {"column":"Status","operator":"eq","value":"ok","logic":"and"}
                ]
                """,
            OutputVariable = "Out",
        }, vars, null);
        var table = FlowTable.Parse(vars["Out"]);
        Assert.Single(table.Rows);
        Assert.Equal("80", table.Rows[0][0]);
    }

    [Fact]
    public void Join_numeric_gte_comparison()
    {
        var vars = new RunVariables
        {
            ["L"] = "{\"columns\":[\"Threshold\"],\"rows\":[[\"8\"]]}",
            ["R"] = "{\"columns\":[\"Level\",\"Label\"],\"rows\":[[\"5\",\"five\"],[\"10\",\"ten\"]]}",
        };
        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.JoinDatatable,
            DataTableVariable = "L",
            SecondDataTableVariable = "R",
            JoinColumn = "Threshold",
            JoinColumn2 = "Level",
            JoinComparisonOperator = VariablesEnums.FilterOperator.Gte,
            JoinOperation = VariablesEnums.JoinOperation.Inner,
            OutputVariable = "J",
        }, vars, null);
        var joined = FlowTable.Parse(vars["J"]);
        Assert.Single(joined.Rows);
        Assert.Equal("five", joined.Rows[0][1]);
    }

    [Fact]
    public void Join_different_column_names()
    {
        var vars = new RunVariables
        {
            ["L"] = "{\"columns\":[\"Id\",\"Name\"],\"rows\":[[\"1\",\"a\"]]}",
            ["R"] = "{\"columns\":[\"KeyId\",\"Team\"],\"rows\":[[\"1\",\"Eng\"]]}",
        };
        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.JoinDatatable,
            DataTableVariable = "L",
            SecondDataTableVariable = "R",
            JoinColumn = "Id",
            JoinColumn2 = "KeyId",
            JoinOperation = VariablesEnums.JoinOperation.Inner,
            OutputVariable = "J",
        }, vars, null);
        var joined = FlowTable.Parse(vars["J"]);
        Assert.Single(joined.Rows);
        Assert.Equal("Team", joined.Columns[2]);
        Assert.Equal("Eng", joined.Rows[0][2]);
    }

    [Fact]
    public void Truncate_number_rounds()
    {
        var vars = new RunVariables { ["n"] = "3.14159" };
        VariablesContext.Execute(new VariablesAction
        {
            Operation = VariablesActionTypes.TruncateNumber,
            SourceVariable = "n",
            Mode = VariablesEnums.TruncateOperation.Round,
            DecimalPlaces = "2",
            OutputVariable = "r",
        }, vars, null);

        Assert.Equal("3.14", vars["r"]);
    }
}
