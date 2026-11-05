using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public bool ShowActionExcel => GetSelectedNode()?.Action is ExcelAction;

    ExcelAction? ExcelA() => A<ExcelAction>();

    void PatchExcel(Func<ExcelAction, ExcelActionPatch> patchFields)
    {
        if (ExcelA() is not { } a)
            return;
        SetAction(ExcelActionMutator.With(a, patchFields(a)));
    }

    public string ExcelInstance
    {
        get => ExcelA()?.Instance ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { Instance = value });
    }

    public string ExcelPath
    {
        get => ExcelA()?.Path ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { Path = value });
    }

    public string ExcelWorksheetName
    {
        get => ExcelA()?.WorksheetName ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { WorksheetName = value });
    }

    public string ExcelWorksheetIndex
    {
        get => ExcelA()?.WorksheetIndex ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { WorksheetIndex = value });
    }

    public string ExcelStartRow
    {
        get => ExcelA()?.StartRow ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { StartRow = value });
    }

    public string ExcelStartColumn
    {
        get => ExcelA()?.StartColumn ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { StartColumn = value });
    }

    public string ExcelEndRow
    {
        get => ExcelA()?.EndRow ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { EndRow = value });
    }

    public string ExcelEndColumn
    {
        get => ExcelA()?.EndColumn ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { EndColumn = value });
    }

    public string ExcelCellName
    {
        get => ExcelA()?.CellName ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { CellName = value });
    }

    public string ExcelValue
    {
        get => ExcelA()?.Value ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { Value = value });
    }

    public string ExcelOutputVariable
    {
        get => ExcelA()?.OutputVariable ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || ExcelA() is not { } a)
                return;
            ApplyProducerRenameOr(
                node,
                () => node.Action = ExcelActionMutator.With(a, new ExcelActionPatch { OutputVariable = value }),
                a.OutputVariable,
                value);
        }
    }

    public string ExcelOutputVariable2
    {
        get => ExcelA()?.OutputVariable2 ?? "";
        set
        {
            if (GetSelectedNode() is not { } node || ExcelA() is not { } a)
                return;
            ApplyProducerRenameOr(
                node,
                () => node.Action = ExcelActionMutator.With(a, new ExcelActionPatch { OutputVariable2 = value }),
                a.OutputVariable2,
                value);
        }
    }

    public string ExcelIndex
    {
        get => ExcelA()?.Index ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { Index = value });
    }

    public string ExcelMacro
    {
        get => ExcelA()?.Macro ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { Macro = value });
    }

    public string ExcelDocumentName
    {
        get => ExcelA()?.DocumentName ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { DocumentName = value });
    }

    public string ExcelNewName
    {
        get => ExcelA()?.NewName ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { NewName = value });
    }

    public string ExcelFindText
    {
        get => ExcelA()?.FindText ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { FindText = value });
    }

    public string ExcelReplaceText
    {
        get => ExcelA()?.ReplaceText ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { ReplaceText = value });
    }

    public string ExcelColorHex
    {
        get => ExcelA()?.ColorHex ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { ColorHex = value });
    }

    public bool ExcelVisible
    {
        get => ExcelA()?.Visible ?? true;
        set => PatchExcel(_ => new ExcelActionPatch { Visible = value });
    }

    public bool ExcelSaveOnClose
    {
        get => ExcelA()?.SaveOnClose ?? false;
        set => PatchExcel(_ => new ExcelActionPatch { SaveOnClose = value });
    }

    public string ExcelMode
    {
        get => ExcelA()?.Mode ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { Mode = value });
    }

    public string ExcelTargetInstance
    {
        get => ExcelA()?.TargetInstance ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { TargetInstance = value });
    }

    public string ExcelSourceWorksheetName
    {
        get => ExcelA()?.SourceWorksheetName ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { SourceWorksheetName = value });
    }

    public string ExcelTargetWorksheetName
    {
        get => ExcelA()?.TargetWorksheetName ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { TargetWorksheetName = value });
    }

    public string ExcelWorksheetPosition
    {
        get => ExcelA()?.WorksheetPosition ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { WorksheetPosition = value });
    }

    public bool ExcelAdvancedFilter
    {
        get => ExcelA()?.AdvancedFilter ?? false;
        set => PatchExcel(_ => new ExcelActionPatch { AdvancedFilter = value });
    }

    public string ExcelFilterOperator1
    {
        get => ExcelA()?.FilterOperator1 ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { FilterOperator1 = value });
    }

    public string ExcelFilterValue1
    {
        get => ExcelA()?.FilterValue1 ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { FilterValue1 = value });
    }

    public string ExcelFilterOperator2
    {
        get => ExcelA()?.FilterOperator2 ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { FilterOperator2 = value });
    }

    public string ExcelFilterValue2
    {
        get => ExcelA()?.FilterValue2 ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { FilterValue2 = value });
    }

    public string ExcelFilterLogic
    {
        get => ExcelA()?.FilterLogic ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { FilterLogic = value });
    }

    public string ExcelPasteMode
    {
        get => ExcelA()?.PasteMode ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { PasteMode = value });
    }

    public string ExcelDeleteScope
    {
        get => ExcelA()?.DeleteScope ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { DeleteScope = value });
    }

    public string ExcelClearFilterScope
    {
        get => ExcelA()?.ClearFilterScope ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { ClearFilterScope = value });
    }

    public string ExcelSortColumn2
    {
        get => ExcelA()?.SortColumn2 ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { SortColumn2 = value });
    }

    public string ExcelSortOrder2
    {
        get => ExcelA()?.SortOrder2 ?? "";
        set => PatchExcel(_ => new ExcelActionPatch { SortOrder2 = value });
    }

    public bool ExcelMatchCase
    {
        get => ExcelA()?.MatchCase ?? false;
        set => PatchExcel(_ => new ExcelActionPatch { MatchCase = value });
    }

    public bool ExcelMatchEntireCell
    {
        get => ExcelA()?.MatchEntireCell ?? false;
        set => PatchExcel(_ => new ExcelActionPatch { MatchEntireCell = value });
    }

    public bool ExcelMatchExactLookup
    {
        get => ExcelA()?.MatchExact ?? true;
        set => PatchExcel(_ => new ExcelActionPatch { MatchExact = value });
    }

    void RefreshExcelInspector()
    {
        OnPropertyChanged(nameof(ShowActionExcel));
        foreach (var name in new[]
                 {
                     nameof(ExcelInstance), nameof(ExcelPath), nameof(ExcelWorksheetName), nameof(ExcelWorksheetIndex),
                     nameof(ExcelStartRow), nameof(ExcelStartColumn), nameof(ExcelEndRow), nameof(ExcelEndColumn),
                     nameof(ExcelCellName), nameof(ExcelValue), nameof(ExcelOutputVariable), nameof(ExcelOutputVariable2),
                     nameof(ExcelIndex), nameof(ExcelMacro), nameof(ExcelDocumentName), nameof(ExcelNewName),
                     nameof(ExcelFindText), nameof(ExcelReplaceText), nameof(ExcelColorHex), nameof(ExcelVisible),
                     nameof(ExcelSaveOnClose), nameof(ExcelMode), nameof(ExcelTargetInstance),
                     nameof(ExcelSourceWorksheetName), nameof(ExcelTargetWorksheetName), nameof(ExcelWorksheetPosition),
                     nameof(ExcelAdvancedFilter), nameof(ExcelFilterOperator1), nameof(ExcelFilterValue1),
                     nameof(ExcelFilterOperator2), nameof(ExcelFilterValue2), nameof(ExcelFilterLogic),
                     nameof(ExcelPasteMode), nameof(ExcelDeleteScope), nameof(ExcelClearFilterScope),
                     nameof(ExcelSortColumn2), nameof(ExcelSortOrder2), nameof(ExcelMatchCase),
                     nameof(ExcelMatchEntireCell), nameof(ExcelMatchExactLookup),
                 })
            OnPropertyChanged(name);
    }
}
