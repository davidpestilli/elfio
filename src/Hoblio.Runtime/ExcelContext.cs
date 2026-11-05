using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Hoblio.Model;

#pragma warning disable CA1416

namespace Hoblio.Runtime;

public sealed class ExcelContext : IDisposable
{
    readonly Dictionary<string, ExcelSession> _sessions = new(StringComparer.Ordinal);

    public void Dispose()
    {
        foreach (var session in _sessions.Values)
            session.Dispose();
        _sessions.Clear();
    }

    public string Execute(ExcelAction action, RunVariables vars, string? projectRoot)
    {
        return action.Type switch
        {
            ExcelActionTypes.Launch => Launch(action, vars, projectRoot),
            ExcelActionTypes.Attach => Attach(action, vars),
            ExcelActionTypes.Read => Read(action, vars),
            ExcelActionTypes.Write => Write(action, vars),
            ExcelActionTypes.Save => Save(action, vars, projectRoot),
            ExcelActionTypes.Close => Close(action, vars),
            ExcelActionTypes.SetActiveWorksheet => SetActiveWorksheet(action, vars),
            ExcelActionTypes.AddWorksheet => AddWorksheet(action, vars),
            ExcelActionTypes.GetActiveCell => GetActiveCell(action, vars),
            ExcelActionTypes.GetFirstFreeColumnRow => GetFirstFreeColumnRow(action, vars),
            ExcelActionTypes.GetColumnName => GetColumnName(action, vars),
            ExcelActionTypes.RunMacro => RunMacro(action, vars),
            ExcelActionTypes.GetActiveWorksheet => GetActiveWorksheet(action, vars),
            ExcelActionTypes.InsertRow => InsertRow(action, vars),
            ExcelActionTypes.DeleteRow => DeleteRow(action, vars),
            ExcelActionTypes.InsertColumn => InsertColumn(action, vars),
            ExcelActionTypes.DeleteColumn => DeleteColumn(action, vars),
            ExcelActionTypes.FindReplace => FindReplace(action, vars),
            ExcelActionTypes.ClearCells => ClearRange(action, vars),
            ExcelActionTypes.SetColor => SetColor(action, vars),
            ExcelActionTypes.SortCells => SortCells(action, vars),
            ExcelActionTypes.FilterCells => FilterCells(action, vars),
            ExcelActionTypes.ClearFilter => ClearFilter(action, vars),
            ExcelActionTypes.GetEmptyCell => GetEmptyCell(action, vars),
            ExcelActionTypes.ResizeColumnsOrRows => ResizeColumnsOrRows(action, vars),
            ExcelActionTypes.GetAllWorksheets => GetAllWorksheets(action, vars),
            ExcelActionTypes.DeleteWorksheet => DeleteWorksheet(action, vars),
            ExcelActionTypes.RenameWorksheet => RenameWorksheet(action, vars),
            ExcelActionTypes.CopyWorksheet => CopyWorksheet(action, vars),
            ExcelActionTypes.ActivateCell => ActivateCell(action, vars),
            ExcelActionTypes.SelectCells => SelectCells(action, vars),
            ExcelActionTypes.GetSelectedRange => GetSelectedRange(action, vars),
            ExcelActionTypes.CopyCells => CopyCells(action, vars),
            ExcelActionTypes.PasteCells => PasteCells(action, vars),
            ExcelActionTypes.DeleteCells => DeleteCells(action, vars),
            ExcelActionTypes.GetFirstFreeRowOnColumn => GetFirstFreeRowOnColumn(action, vars),
            ExcelActionTypes.ReadCellFormula => ReadCellFormula(action, vars),
            ExcelActionTypes.GetTableRange => GetTableRange(action, vars),
            ExcelActionTypes.AutofillCells => AutofillCells(action, vars),
            ExcelActionTypes.AppendCells => AppendCells(action, vars),
            ExcelActionTypes.LookupRange => LookupRange(action, vars),
            _ => throw new NotImplementedException(
                $"Excel action `{ExcelActionTypes.ModuleActionId(action.Type)}` ainda não implementada no runtime."),
        };
    }

    const int XlShiftToLeft = -4159;
    const int XlShiftUp = -4162;
    const int XlPasteAll = -4104;

    string Launch(ExcelAction action, RunVariables vars, string? projectRoot)
    {
        var instanceVar = RequireInstanceVar(action);
        var visible = action.Visible ?? true;
        dynamic app = Activator.CreateInstance(Type.GetTypeFromProgID("Excel.Application")!)
            ?? throw new InvalidOperationException("Microsoft Excel não está instalado ou registrado.");

        app.Visible = visible;
        app.DisplayAlerts = false;

        if (!string.IsNullOrWhiteSpace(action.Path))
        {
            var path = RunVariables.Interpolate(action.Path, vars);
            if (!string.IsNullOrWhiteSpace(path))
            {
                path = ResolvePath(path, projectRoot);
                app.Workbooks.Open(path, ReadOnly: action.ReadOnly ?? false);
            }
        }
        else if (app.Workbooks.Count == 0)
            app.Workbooks.Add();

        var session = ExcelSession.Attach(app);
        BindSession(instanceVar, session, vars);
        return $"excel_launch: instance `{instanceVar}` visible={visible}";
    }

    string Attach(ExcelAction action, RunVariables vars)
    {
        var instanceVar = RequireInstanceVar(action);
        dynamic app = ComActiveObject.Get("Excel.Application");
        var docName = action.DocumentName is null ? null : RunVariables.Interpolate(action.DocumentName, vars);
        if (!string.IsNullOrWhiteSpace(docName))
        {
            var matched = false;
            foreach (dynamic wb in app.Workbooks)
            {
                var name = (string)wb.Name;
                if (name.Contains(docName, StringComparison.OrdinalIgnoreCase))
                {
                    wb.Activate();
                    matched = true;
                    break;
                }
            }
            if (!matched)
                throw new InvalidOperationException($"Nenhuma pasta de trabalho contém `{docName}`.");
        }

        BindSession(instanceVar, ExcelSession.Attach(app), vars);
        return $"excel_attach: instance `{instanceVar}`";
    }

    string Read(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var sheet = ResolveWorksheet(session, action, vars);
        var output = RequireOutput(action);
        object? value;
        if (!string.IsNullOrWhiteSpace(action.CellName))
        {
            var cell = RunVariables.Interpolate(action.CellName!, vars);
            value = sheet.Range[cell].Value2;
        }
        else
        {
            var row = ParseInt(action.StartRow, 1);
            var col = ParseColumn(action.StartColumn, 1);
            value = sheet.Cells[row, col].Value2;
        }

        vars[output] = FormatCellValue(value);
        return $"excel_read: `{output}` = {Truncate(vars[output], 80)}";
    }

    string Write(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var sheet = ResolveWorksheet(session, action, vars);
        var text = RunVariables.Interpolate(action.Value ?? "", vars);
        if (!string.IsNullOrWhiteSpace(action.CellName))
        {
            var cell = RunVariables.Interpolate(action.CellName!, vars);
            sheet.Range[cell].Value2 = text;
        }
        else
        {
            var row = ParseInt(action.StartRow, 1);
            var col = ParseColumn(action.StartColumn, 1);
            sheet.Cells[row, col].Value2 = text;
        }

        return $"excel_write: {text.Length} chars";
    }

    string Save(ExcelAction action, RunVariables vars, string? projectRoot)
    {
        var session = RequireSession(action, vars);
        dynamic wb = session.App.ActiveWorkbook
            ?? throw new InvalidOperationException("Nenhuma pasta de trabalho ativa.");
        if (!string.IsNullOrWhiteSpace(action.Path))
        {
            var path = ResolvePath(RunVariables.Interpolate(action.Path!, vars), projectRoot);
            wb.SaveAs(path);
            return $"excel_save: SaveAs `{path}`";
        }

        wb.Save();
        return "excel_save: saved";
    }

    string Close(ExcelAction action, RunVariables vars)
    {
        var instanceVar = action.Instance.Trim();
        var session = RequireSession(action, vars);
        var save = action.SaveOnClose ?? false;
        session.App.DisplayAlerts = false;
        if (save)
            session.App.ActiveWorkbook?.Save();
        session.App.Quit();
        session.Dispose();
        _sessions.Remove(session.Id);
        vars.Remove(instanceVar);
        return $"excel_close: `{instanceVar}` closed save={save}";
    }

    string SetActiveWorksheet(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        dynamic wb = session.App.ActiveWorkbook
            ?? throw new InvalidOperationException("Nenhuma pasta de trabalho ativa.");
        if (!string.IsNullOrWhiteSpace(action.WorksheetIndex))
        {
            var index = ParseInt(action.WorksheetIndex, 1);
            wb.Worksheets[index].Activate();
        }
        else
        {
            var name = RunVariables.Interpolate(action.WorksheetName ?? "Sheet1", vars);
            wb.Worksheets[name].Activate();
        }

        return "excel_set_active_worksheet: ok";
    }

    string AddWorksheet(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        dynamic wb = session.App.ActiveWorkbook
            ?? throw new InvalidOperationException("Nenhuma pasta de trabalho ativa.");
        dynamic ws = wb.Worksheets.Add(After: wb.Worksheets[wb.Worksheets.Count]);
        if (!string.IsNullOrWhiteSpace(action.NewName))
            ws.Name = RunVariables.Interpolate(action.NewName!, vars);
        ws.Activate();
        return $"excel_add_worksheet: {ws.Name}";
    }

    string GetActiveCell(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var output = RequireOutput(action);
        var addr = (string)session.App.ActiveCell.AddressLocal;
        vars[output] = addr;
        return $"excel_get_active_cell: `{output}` = {addr}";
    }

    string GetFirstFreeColumnRow(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var sheet = ResolveWorksheet(session, action, vars);
        dynamic used = sheet.UsedRange;
        var row = (int)used.Row + (int)used.Rows.Count;
        var col = (int)used.Column;
        if ((string?)sheet.Cells[row, col].Value2 is not null)
            col++;
        if (!string.IsNullOrWhiteSpace(action.OutputVariable))
            vars[action.OutputVariable.Trim()] = row.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(action.OutputVariable2))
            vars[action.OutputVariable2.Trim()] = ColumnLetter(col);
        return "excel_get_first_free_column_row: ok";
    }

    string GetColumnName(ExcelAction action, RunVariables vars)
    {
        var output = RequireOutput(action);
        var index = ParseInt(action.Index, 1);
        vars[output] = ColumnLetter(index);
        return $"excel_get_column_name: `{output}` = {vars[output]}";
    }

    string RunMacro(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var macro = RunVariables.Interpolate(action.Macro ?? "", vars);
        if (string.IsNullOrWhiteSpace(macro))
            throw new InvalidOperationException("excel_run_macro: macro vazia.");
        session.App.Run(macro);
        return $"excel_run_macro: {macro}";
    }

    string GetActiveWorksheet(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var output = RequireOutput(action);
        var name = (string)session.App.ActiveSheet.Name;
        vars[output] = name;
        return $"excel_get_active_worksheet: `{output}` = {name}";
    }

    string InsertRow(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var index = ParseInt(action.Index, 1);
        sheet.Rows[index].Insert();
        return $"excel_insert_row: {index}";
    }

    string DeleteRow(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var index = ParseInt(action.Index, 1);
        sheet.Rows[index].Delete();
        return $"excel_delete_row: {index}";
    }

    string InsertColumn(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var index = ParseColumn(action.Index, 1);
        sheet.Columns[index].Insert();
        return $"excel_insert_column: {index}";
    }

    string DeleteColumn(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var index = ParseColumn(action.Index, 1);
        sheet.Columns[index].Delete();
        return $"excel_delete_column: {index}";
    }

    string FindReplace(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var find = RunVariables.Interpolate(action.FindText ?? "", vars);
        var replace = RunVariables.Interpolate(action.ReplaceText ?? "", vars);
        var sheet = ResolveWorksheet(session, action, vars);
        dynamic cells = sheet.Cells;
        var matchCase = action.MatchCase ?? false;
        var entire = action.MatchEntireCell ?? false;
        dynamic found = cells.Find(
            find,
            LookAt: entire ? 2 : 1,
            MatchCase: matchCase);
        var count = 0;
        while (found is not null)
        {
            found.Value2 = replace;
            count++;
            found = cells.Find(
                find,
                After: found,
                LookAt: entire ? 2 : 1,
                MatchCase: matchCase);
            if (count > 10_000)
                break;
        }

        return $"excel_find_replace: {count} ocorrência(s)";
    }

    string ClearRange(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        ResolveRange(sheet, action, vars).Clear();
        return "excel_clear_cells: ok";
    }

    string SetColor(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var range = ResolveRange(sheet, action, vars);
        var hex = (action.ColorHex ?? "#FFFFFF").TrimStart('#');
        if (hex.Length != 6)
            throw new InvalidOperationException("color_hex deve ter 6 dígitos (RRGGBB).");
        var r = Convert.ToInt32(hex[..2], 16);
        var g = Convert.ToInt32(hex[2..4], 16);
        var b = Convert.ToInt32(hex[4..6], 16);
        range.Interior.Color = r + (g << 8) + (b << 16);
        return "excel_set_color: ok";
    }

    string SortCells(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var range = ResolveSortRange(sheet, action, vars);
        var keyCol = ParseColumn(action.StartColumn, (int)range.Column);
        var keyRow = ParseInt(action.StartRow, (int)range.Row);
        dynamic keyCell = sheet.Cells[keyRow, keyCol];
        var descending = ExcelComSemantics.SortDescending(action.Mode);
        if (string.IsNullOrWhiteSpace(action.SortColumn2))
        {
            range.Sort(
                Key1: keyCell,
                Order1: descending ? 2 : 1,
                Header: (action.FirstLineIsHeader ?? false) ? 1 : 0);
            return "excel_sort_cells: ok";
        }

        var key2Col = ParseColumn(action.SortColumn2, keyCol);
        dynamic key2Cell = sheet.Cells[keyRow, key2Col];
        var desc2 = ExcelComSemantics.SortDescending(action.SortOrder2);
        range.Sort(
            Key1: keyCell,
            Order1: descending ? 2 : 1,
            Key2: key2Cell,
            Order2: desc2 ? 2 : 1,
            Header: (action.FirstLineIsHeader ?? false) ? 1 : 0);
        return "excel_sort_cells: ok (2 keys)";
    }

    string FilterCells(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var range = ResolveSortRange(sheet, action, vars);
        var field = ParseColumn(action.StartColumn, (int)range.Column) - (int)range.Column + 1;
        if (field < 1)
            field = 1;

        var useAdvanced = action.AdvancedFilter == true
            || !string.IsNullOrWhiteSpace(action.FilterOperator1)
            || !string.IsNullOrWhiteSpace(action.FilterValue1);

        if (!useAdvanced)
        {
            var criteria = RunVariables.Interpolate(action.Value ?? action.FilterValue1 ?? "", vars);
            if (string.IsNullOrWhiteSpace(criteria))
                criteria = "*";
            else if (!criteria.Contains('*', StringComparison.Ordinal) && !criteria.Contains('=', StringComparison.Ordinal))
                criteria = $"={criteria}";
            range.AutoFilter(Field: field, Criteria1: criteria);
            return $"excel_filter_cells: field={field} criteria={criteria}";
        }

        var c1 = ExcelComSemantics.BuildFilterCriteria(
            action.FilterOperator1,
            action.FilterValue1 ?? action.Value ?? "",
            vars);
        var logic = ExcelComSemantics.FilterLogicOperator(action.FilterLogic);
        if (string.IsNullOrWhiteSpace(action.FilterValue2))
        {
            range.AutoFilter(Field: field, Criteria1: c1);
            return $"excel_filter_cells: advanced field={field} c1={c1}";
        }

        var c2 = ExcelComSemantics.BuildFilterCriteria(action.FilterOperator2, action.FilterValue2!, vars);
        range.AutoFilter(Field: field, Criteria1: c1, Operator: logic, Criteria2: c2);
        return $"excel_filter_cells: advanced field={field} c1={c1} op={logic} c2={c2}";
    }

    string ClearFilter(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var scope = (action.ClearFilterScope ?? ExcelEnums.ClearFilterScope.All).Trim().ToLowerInvariant();
        if (scope is ExcelEnums.ClearFilterScope.Table or "table" || !string.IsNullOrWhiteSpace(action.TableName))
        {
            var table = RunVariables.Interpolate(action.TableName ?? "", vars);
            if (!string.IsNullOrWhiteSpace(table))
            {
                dynamic lo = sheet.ListObjects[table];
                if (lo.ShowAutoFilter)
                    lo.Range.AutoFilter();
                return "excel_clear_filter: table";
            }
        }

        if (scope is ExcelEnums.ClearFilterScope.Column or "column" && !string.IsNullOrWhiteSpace(action.StartColumn))
        {
            var field = ParseColumn(action.StartColumn, 1);
            if (sheet.AutoFilterMode)
                sheet.Range.AutoFilter(Field: field);
            return "excel_clear_filter: column";
        }

        if (sheet.AutoFilterMode)
            sheet.ShowAllData();
        return "excel_clear_filter: all";
    }

    string GetEmptyCell(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var output = RequireOutput(action);
        var startRow = ParseInt(action.StartRow, 1);
        var startCol = ParseColumn(action.StartColumn, 1);
        var direction = (action.Mode ?? "down").Trim().ToLowerInvariant();
        (int row, int col) coords = direction is "right" or "row"
            ? FindEmptyByRowScan(sheet, startRow, startCol)
            : FindEmptyByColumnScan(sheet, startRow, startCol);
        var row = coords.row;
        var col = coords.col;
        vars[output] = $"{ColumnLetter(col)}{row}";
        return $"excel_get_empty_cell: `{output}` = {vars[output]}";
    }

    string ResizeColumnsOrRows(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var size = ParseDouble(action.Value, 10);
        var target = (action.Mode ?? "column").Trim().ToLowerInvariant();
        if (target is "row" or "rows")
        {
            var row = ParseInt(action.Index ?? action.StartRow, 1);
            sheet.Rows[row].RowHeight = size;
            return $"excel_resize_columns_or_rows: row {row} height={size}";
        }

        var col = ParseColumn(action.Index ?? action.StartColumn, 1);
        sheet.Columns[col].ColumnWidth = size;
        return $"excel_resize_columns_or_rows: col {ColumnLetter(col)} width={size}";
    }

    string GetAllWorksheets(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        dynamic wb = session.App.ActiveWorkbook
            ?? throw new InvalidOperationException("Nenhuma pasta de trabalho ativa.");
        var names = new List<string>();
        foreach (dynamic ws in wb.Worksheets)
            names.Add((string)ws.Name);
        var output = RequireOutput(action);
        vars[output] = JsonSerializer.Serialize(names);
        return $"excel_get_all_worksheets: {names.Count} planilha(s)";
    }

    string DeleteWorksheet(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        dynamic wb = session.App.ActiveWorkbook
            ?? throw new InvalidOperationException("Nenhuma pasta de trabalho ativa.");
        if (wb.Worksheets.Count <= 1)
            throw new InvalidOperationException("Não é possível excluir a única planilha.");
        ResolveWorksheet(session, action, vars).Delete();
        return "excel_delete_worksheet: ok";
    }

    string RenameWorksheet(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var newName = RunVariables.Interpolate(action.NewName ?? action.Value ?? "", vars);
        if (string.IsNullOrWhiteSpace(newName))
            throw new InvalidOperationException("excel_rename_worksheet: new_name vazio.");
        sheet.Name = newName;
        return $"excel_rename_worksheet: {newName}";
    }

    string CopyWorksheet(ExcelAction action, RunVariables vars)
    {
        var sourceSession = RequireSession(action, vars);
        var targetVar = string.IsNullOrWhiteSpace(action.TargetInstance)
            ? action.Instance.Trim()
            : action.TargetInstance.Trim();
        var targetSession = string.Equals(targetVar, action.Instance.Trim(), StringComparison.Ordinal)
            ? sourceSession
            : RequireSessionForVariable(targetVar, vars);

        dynamic sourceWb = sourceSession.App.ActiveWorkbook
            ?? throw new InvalidOperationException("Pasta de origem não ativa.");
        dynamic targetWb = targetSession.App.ActiveWorkbook
            ?? throw new InvalidOperationException("Pasta de destino não ativa.");

        var sourceSheet = ResolveWorksheetOnWorkbook(sourceSession, sourceWb, action, vars);
        var position = (action.WorksheetPosition ?? ExcelEnums.WorksheetPosition.After).Trim().ToLowerInvariant();
        dynamic anchor = position switch
        {
            ExcelEnums.WorksheetPosition.Before or "before" => targetWb.Worksheets[1],
            ExcelEnums.WorksheetPosition.Begin or "begin" => targetWb.Worksheets[1],
            ExcelEnums.WorksheetPosition.End or "end" => targetWb.Worksheets[targetWb.Worksheets.Count],
            _ => targetWb.Worksheets[targetWb.Worksheets.Count],
        };

        if (position is ExcelEnums.WorksheetPosition.Before or ExcelEnums.WorksheetPosition.Begin or "before" or "begin")
            sourceSheet.Copy(Before: anchor);
        else
            sourceSheet.Copy(After: anchor);

        dynamic copied = targetSession.App.ActiveSheet;
        var rename = action.NewName ?? action.TargetWorksheetName;
        if (!string.IsNullOrWhiteSpace(rename))
            copied.Name = RunVariables.Interpolate(rename, vars);
        return $"excel_copy_worksheet: {copied.Name}";
    }

    string ActivateCell(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var cell = ResolveRange(sheet, action, vars);
        cell.Select();
        return "excel_activate_cell: ok";
    }

    string SelectCells(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        ResolveRange(sheet, action, vars).Select();
        return "excel_select_cells: ok";
    }

    string GetSelectedRange(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        dynamic selection = session.App.Selection;
        var output = RequireOutput(action);
        vars[output] = (string)selection.AddressLocal;
        if (!string.IsNullOrWhiteSpace(action.OutputVariable2))
        {
            vars[action.OutputVariable2.Trim()] =
                $"{selection.Row},{selection.Column},{selection.Row + selection.Rows.Count - 1},{selection.Column + selection.Columns.Count - 1}";
        }

        return $"excel_get_selected_range: `{output}` = {vars[output]}";
    }

    string CopyCells(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var range = ResolveRange(sheet, action, vars);
        var mode = (action.CopyMode ?? "default").Trim().ToLowerInvariant();
        if (mode is "values" or "formulas")
        {
            range.Copy();
            return $"excel_copy_cells: clipboard ({mode})";
        }

        range.Copy();
        return "excel_copy_cells: copied to clipboard";
    }

    string PasteCells(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var sheet = ResolveWorksheet(session, action, vars);
        var dest = ResolveRange(sheet, action, vars);
        var pasteType = ExcelComSemantics.ParsePasteType(action.PasteMode ?? action.Mode);
        dest.PasteSpecial(pasteType);
        return $"excel_paste_cells: paste_type={pasteType}";
    }

    string DeleteCells(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var range = ResolveRange(sheet, action, vars);
        var scope = (action.DeleteScope ?? ExcelEnums.DeleteScope.Cells).Trim().ToLowerInvariant();
        if (scope is ExcelEnums.DeleteScope.Rows or "rows")
        {
            range.EntireRow.Delete();
            return "excel_delete_cells: rows";
        }

        if (scope is ExcelEnums.DeleteScope.Columns or "columns")
        {
            range.EntireColumn.Delete();
            return "excel_delete_cells: columns";
        }

        var shift = string.Equals(action.Mode, "up", StringComparison.OrdinalIgnoreCase) ? XlShiftUp : XlShiftToLeft;
        range.Delete(Shift: shift);
        return "excel_delete_cells: cells";
    }

    string GetFirstFreeRowOnColumn(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var col = ParseColumn(action.StartColumn, 1);
        var row = 1;
        var lastUsed = 0;
        while (row <= 1_048_576)
        {
            if (!IsCellEmpty(sheet.Cells[row, col]))
                lastUsed = row;
            else if (lastUsed > 0)
                break;
            row++;
            if (row > 100_000 && lastUsed == 0)
                break;
        }

        var freeRow = lastUsed == 0 ? 1 : lastUsed + 1;
        var output = RequireOutput(action);
        vars[output] = freeRow.ToString(CultureInfo.InvariantCulture);
        return $"excel_get_first_free_row_on_column: `{output}` = {freeRow}";
    }

    string ReadCellFormula(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var cell = ResolveRange(sheet, action, vars);
        var output = RequireOutput(action);
        vars[output] = cell.Formula?.ToString() ?? "";
        return $"excel_read_cell_formula: `{output}`";
    }

    string GetTableRange(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var table = RunVariables.Interpolate(action.TableName ?? "", vars);
        if (string.IsNullOrWhiteSpace(table))
            throw new InvalidOperationException("excel_get_table_range: table_name obrigatório.");
        dynamic lo = sheet.ListObjects[table];
        dynamic rng = lo.Range;
        var output = RequireOutput(action);
        vars[output] = (string)rng.AddressLocal;
        if (!string.IsNullOrWhiteSpace(action.OutputVariable2))
        {
            vars[action.OutputVariable2.Trim()] =
                $"{rng.Row},{rng.Column},{rng.Row + rng.Rows.Count - 1},{rng.Column + rng.Columns.Count - 1}";
        }

        return $"excel_get_table_range: `{output}` = {vars[output]}";
    }

    string AutofillCells(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var source = ResolveRange(sheet, action, vars);
        var destEndRow = ParseInt(action.EndRow, (int)source.Row + (int)source.Rows.Count);
        var destEndCol = ParseColumn(action.EndColumn, (int)source.Column + (int)source.Columns.Count - 1);
        dynamic dest = sheet.Range[
            sheet.Cells[(int)source.Row, (int)source.Column],
            sheet.Cells[destEndRow, destEndCol]];
        source.AutoFill(dest);
        return "excel_autofill_cells: ok";
    }

    string AppendCells(ExcelAction action, RunVariables vars)
    {
        var sheet = ResolveWorksheet(RequireSession(action, vars), action, vars);
        var col = ParseColumn(action.StartColumn, 1);
        var startRow = ParseInt(action.StartRow, 0);
        if (startRow <= 0)
        {
            startRow = 1;
            while (startRow <= 1_048_576 && !IsCellEmpty(sheet.Cells[startRow + 1, col]))
                startRow++;
            startRow++;
        }

        var text = RunVariables.Interpolate(action.Value ?? "", vars);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (action.FirstLineIsHeader == true && startRow == 1 && lines.Length > 0)
        {
            sheet.Cells[startRow, col].Value2 = lines[0];
            startRow++;
            lines = lines[1..];
        }

        foreach (var line in lines)
        {
            sheet.Cells[startRow, col].Value2 = line;
            startRow++;
        }

        return $"excel_append_cells: {lines.Length} linha(s)";
    }

    string LookupRange(ExcelAction action, RunVariables vars)
    {
        var session = RequireSession(action, vars);
        var sheet = ResolveWorksheet(session, action, vars);
        var lookup = RunVariables.Interpolate(action.Value ?? "", vars);
        var r1 = ParseInt(action.StartRow, 1);
        var c1 = ParseColumn(action.StartColumn, 1);
        var r2 = ParseInt(action.EndRow, r1);
        var c2 = ParseColumn(action.EndColumn, c1);
        dynamic table = sheet.Range[sheet.Cells[r1, c1], sheet.Cells[r2, c2]];
        var index = ParseInt(action.Index, 2);
        var mode = (action.Mode ?? "vlookup").Trim().ToLowerInvariant();
        var exact = action.MatchExact ?? true;
        dynamic fn = session.App.WorksheetFunction;
        object? result = mode switch
        {
            "hlookup" => fn.HLookup(lookup, table, index, exact),
            _ => fn.VLookup(lookup, table, index, exact),
        };
        var output = RequireOutput(action);
        vars[output] = FormatCellValue(result);
        return $"excel_lookup_range: `{output}` = {Truncate(vars[output], 80)}";
    }

    void BindSession(string instanceVar, ExcelSession session, RunVariables vars)
    {
        _sessions[session.Id] = session;
        vars[instanceVar] = session.Id;
    }

    ExcelSession RequireSession(ExcelAction action, RunVariables vars) =>
        RequireSessionForVariable(action.Instance.Trim(), vars);

    ExcelSession RequireSessionForVariable(string instanceVar, RunVariables vars)
    {
        if (string.IsNullOrEmpty(instanceVar))
            throw new InvalidOperationException("Excel: variável de instância vazia.");
        if (!vars.TryGetValue(instanceVar, out var sessionId) || string.IsNullOrWhiteSpace(sessionId))
            throw new InvalidOperationException($"Variável `{instanceVar}` não contém sessão Excel. Use Iniciar ou Anexar antes.");
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new InvalidOperationException($"Sessão Excel `{sessionId}` expirou ou não existe.");
        return session;
    }

    static dynamic ResolveWorksheetOnWorkbook(ExcelSession session, dynamic wb, ExcelAction action, RunVariables vars)
    {
        var name = action.SourceWorksheetName ?? action.WorksheetName;
        if (!string.IsNullOrWhiteSpace(action.WorksheetIndex))
            return wb.Worksheets[ParseInt(action.WorksheetIndex, 1)];
        if (!string.IsNullOrWhiteSpace(name))
            return wb.Worksheets[RunVariables.Interpolate(name, vars)];
        return session.App.ActiveSheet;
    }

    static string RequireInstanceVar(ExcelAction action)
    {
        var name = action.Instance.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Excel: variável de instância vazia.");
        return name;
    }

    static string RequireOutput(ExcelAction action)
    {
        var name = action.OutputVariable?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Excel: output_variable é obrigatório.");
        return name;
    }

    static dynamic ResolveWorksheet(ExcelSession session, ExcelAction action, RunVariables vars)
    {
        dynamic wb = session.App.ActiveWorkbook
            ?? throw new InvalidOperationException("Nenhuma pasta de trabalho ativa.");
        if (!string.IsNullOrWhiteSpace(action.WorksheetIndex))
            return wb.Worksheets[ParseInt(action.WorksheetIndex, 1)];
        if (!string.IsNullOrWhiteSpace(action.WorksheetName))
            return wb.Worksheets[RunVariables.Interpolate(action.WorksheetName!, vars)];
        return session.App.ActiveSheet;
    }

    static dynamic ResolveRange(dynamic sheet, ExcelAction action, RunVariables vars)
    {
        if (!string.IsNullOrWhiteSpace(action.CellName))
            return sheet.Range[RunVariables.Interpolate(action.CellName!, vars)];
        var r1 = ParseInt(action.StartRow, 1);
        var c1 = ParseColumn(action.StartColumn, 1);
        if (!string.IsNullOrWhiteSpace(action.EndRow) || !string.IsNullOrWhiteSpace(action.EndColumn))
        {
            var r2 = ParseInt(action.EndRow, r1);
            var c2 = ParseColumn(action.EndColumn, c1);
            return sheet.Range[sheet.Cells[r1, c1], sheet.Cells[r2, c2]];
        }

        return sheet.Cells[r1, c1];
    }

    static dynamic ResolveSortRange(dynamic sheet, ExcelAction action, RunVariables vars)
    {
        if (!string.IsNullOrWhiteSpace(action.TableName))
            return sheet.ListObjects[RunVariables.Interpolate(action.TableName!, vars)].Range;
        if (!string.IsNullOrWhiteSpace(action.CellName))
            return sheet.Range[RunVariables.Interpolate(action.CellName!, vars)];
        if (!string.IsNullOrWhiteSpace(action.StartRow) || !string.IsNullOrWhiteSpace(action.StartColumn)
            || !string.IsNullOrWhiteSpace(action.EndRow) || !string.IsNullOrWhiteSpace(action.EndColumn))
            return ResolveRange(sheet, action, vars);
        return sheet.UsedRange;
    }

    static (int Row, int Col) FindEmptyByColumnScan(dynamic sheet, int startRow, int startCol)
    {
        for (var col = startCol; col <= startCol + 512; col++)
        {
            for (var row = startRow; row <= startRow + 100_000; row++)
            {
                if (IsCellEmpty(sheet.Cells[row, col]))
                    return (row, col);
            }
        }

        throw new InvalidOperationException("Nenhuma célula vazia encontrada na busca.");
    }

    static (int Row, int Col) FindEmptyByRowScan(dynamic sheet, int startRow, int startCol)
    {
        for (var row = startRow; row <= startRow + 100_000; row++)
        {
            for (var col = startCol; col <= startCol + 512; col++)
            {
                if (IsCellEmpty(sheet.Cells[row, col]))
                    return (row, col);
            }
        }

        throw new InvalidOperationException("Nenhuma célula vazia encontrada na busca.");
    }

    static bool IsCellEmpty(dynamic cell)
    {
        var value = cell.Value2;
        return value is null || (value is string s && string.IsNullOrWhiteSpace(s));
    }

    static double ParseDouble(string? text, double fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;
        return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : fallback;
    }

    static int ParseInt(string? text, int fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;
        return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : fallback;
    }

    static int ParseColumn(string? text, int fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;
        text = text.Trim();
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return n;
        return ColumnIndex(text);
    }

    static int ColumnIndex(string letters)
    {
        var sum = 0;
        foreach (var ch in letters.ToUpperInvariant())
        {
            if (ch is < 'A' or > 'Z')
                throw new InvalidOperationException($"Coluna inválida: `{letters}`.");
            sum = sum * 26 + (ch - 'A' + 1);
        }
        return sum;
    }

    static string ColumnLetter(int index)
    {
        if (index < 1)
            throw new InvalidOperationException("Índice de coluna deve ser >= 1.");
        var letters = "";
        while (index > 0)
        {
            index--;
            letters = (char)('A' + index % 26) + letters;
            index /= 26;
        }
        return letters;
    }

    static string FormatCellValue(object? value) =>
        value switch
        {
            null => "",
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            bool b => b ? "TRUE" : "FALSE",
            _ => value.ToString() ?? "",
        };

    static string ResolvePath(string path, string? projectRoot)
    {
        if (Path.IsPathRooted(path) || string.IsNullOrWhiteSpace(projectRoot))
            return path;
        return Path.GetFullPath(Path.Combine(projectRoot, path));
    }

    static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";

    static class ComActiveObject
    {
        public static object Get(string progId)
        {
            var type = Type.GetTypeFromProgID(progId)
                ?? throw new InvalidOperationException($"ProgID não registrado: {progId}");
            var clsid = type.GUID;
            GetActiveObject(ref clsid, IntPtr.Zero, out var obj);
            return obj;
        }

        [DllImport("oleaut32.dll", PreserveSig = false)]
        static extern void GetActiveObject(
            ref Guid rclsid,
            IntPtr pvReserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
    }

    sealed class ExcelSession : IDisposable
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public dynamic App { get; }

        ExcelSession(dynamic app) => App = app;

        public static ExcelSession Attach(dynamic app) => new(app);

        public void Dispose()
        {
            try
            {
                if (App is not null)
                    Marshal.ReleaseComObject(App);
            }
            catch
            {
                // ignore COM teardown races
            }
        }
    }
}
