using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public bool ShowActionVariables => GetSelectedNode()?.Action is VariablesAction;

    VariablesAction? VarA() => A<VariablesAction>();

    void PatchVar(Func<VariablesAction, VariablesActionPatch> patchFields)
    {
        if (VarA() is not { } a)
            return;
        SetAction(VariablesActionMutator.With(a, patchFields(a)));
    }

    public string VarDataTableVariable
    {
        get => VarA()?.DataTableVariable ?? "";
        set => PatchVar(_ => new VariablesActionPatch { DataTableVariable = value });
    }

    public string VarSecondDataTableVariable
    {
        get => VarA()?.SecondDataTableVariable ?? "";
        set => PatchVar(_ => new VariablesActionPatch { SecondDataTableVariable = value });
    }

    public string VarListVariable
    {
        get => VarA()?.ListVariable ?? "";
        set => PatchVar(_ => new VariablesActionPatch { ListVariable = value });
    }

    public string VarSecondListVariable
    {
        get => VarA()?.SecondListVariable ?? "";
        set => PatchVar(_ => new VariablesActionPatch { SecondListVariable = value });
    }

    public string VarOutputVariable
    {
        get => VarA()?.OutputVariable ?? "";
        set => PatchVar(_ => new VariablesActionPatch { OutputVariable = value });
    }

    public string VarOutputVariable2
    {
        get => VarA()?.OutputVariable2 ?? "";
        set => PatchVar(_ => new VariablesActionPatch { OutputVariable2 = value });
    }

    public string VarValue
    {
        get => VarA()?.Value ?? "";
        set => PatchVar(_ => new VariablesActionPatch { Value = value });
    }

    public string VarValues
    {
        get => VarA()?.Values ?? "";
        set => PatchVar(_ => new VariablesActionPatch { Values = value });
    }

    public string VarItems
    {
        get => VarA()?.Items ?? "";
        set => PatchVar(_ => new VariablesActionPatch { Items = value });
    }

    public string VarColumnName
    {
        get => VarA()?.ColumnName ?? "";
        set => PatchVar(_ => new VariablesActionPatch { ColumnName = value });
    }

    public string VarColumnIndex
    {
        get => VarA()?.ColumnIndex ?? "";
        set => PatchVar(_ => new VariablesActionPatch { ColumnIndex = value });
    }

    public string VarRowIndex
    {
        get => VarA()?.RowIndex ?? "";
        set => PatchVar(_ => new VariablesActionPatch { RowIndex = value });
    }

    public string VarFindText
    {
        get => VarA()?.FindText ?? "";
        set => PatchVar(_ => new VariablesActionPatch { FindText = value });
    }

    public string VarReplaceText
    {
        get => VarA()?.ReplaceText ?? "";
        set => PatchVar(_ => new VariablesActionPatch { ReplaceText = value });
    }

    public string VarMode
    {
        get => VarA()?.Mode ?? "";
        set => PatchVar(_ => new VariablesActionPatch { Mode = value });
    }

    public string VarSortColumn
    {
        get => VarA()?.SortColumn ?? "";
        set => PatchVar(_ => new VariablesActionPatch { SortColumn = value });
    }

    public string VarSortOrder
    {
        get => VarA()?.SortOrder ?? "";
        set => PatchVar(_ => new VariablesActionPatch { SortOrder = value });
    }

    public string VarSortColumn2
    {
        get => VarA()?.SortColumn2 ?? "";
        set => PatchVar(_ => new VariablesActionPatch { SortColumn2 = value });
    }

    public string VarSortOrder2
    {
        get => VarA()?.SortOrder2 ?? "";
        set => PatchVar(_ => new VariablesActionPatch { SortOrder2 = value });
    }

    public string VarColumnWidths
    {
        get => VarA()?.ColumnWidths ?? "";
        set => PatchVar(_ => new VariablesActionPatch { ColumnWidths = value });
    }

    public string VarFilterColumn
    {
        get => VarA()?.FilterColumn ?? "";
        set => PatchVar(_ => new VariablesActionPatch { FilterColumn = value });
    }

    public string VarFilterOperator
    {
        get => VarA()?.FilterOperator ?? "";
        set => PatchVar(_ => new VariablesActionPatch { FilterOperator = value });
    }

    public string VarFilterValue
    {
        get => VarA()?.FilterValue ?? "";
        set => PatchVar(_ => new VariablesActionPatch { FilterValue = value });
    }

    public string VarFilterLogic
    {
        get => VarA()?.FilterLogic ?? "";
        set => PatchVar(_ => new VariablesActionPatch { FilterLogic = value });
    }

    public string VarFilterColumn2
    {
        get => VarA()?.FilterColumn2 ?? "";
        set => PatchVar(_ => new VariablesActionPatch { FilterColumn2 = value });
    }

    public string VarFilterOperator2
    {
        get => VarA()?.FilterOperator2 ?? "";
        set => PatchVar(_ => new VariablesActionPatch { FilterOperator2 = value });
    }

    public string VarFilterValue2
    {
        get => VarA()?.FilterValue2 ?? "";
        set => PatchVar(_ => new VariablesActionPatch { FilterValue2 = value });
    }

    public string VarFilterConditions
    {
        get => VarA()?.FilterConditions ?? "";
        set => PatchVar(_ => new VariablesActionPatch { FilterConditions = value });
    }

    public string VarJoinColumn
    {
        get => VarA()?.JoinColumn ?? "";
        set => PatchVar(_ => new VariablesActionPatch { JoinColumn = value });
    }

    public string VarJoinOperation
    {
        get => VarA()?.JoinOperation ?? "";
        set => PatchVar(_ => new VariablesActionPatch { JoinOperation = value });
    }

    public string VarJoinColumn2
    {
        get => VarA()?.JoinColumn2 ?? "";
        set => PatchVar(_ => new VariablesActionPatch { JoinColumn2 = value });
    }

    public string VarJoinComparisonOperator
    {
        get => VarA()?.JoinComparisonOperator ?? "";
        set => PatchVar(_ => new VariablesActionPatch { JoinComparisonOperator = value });
    }

    public string VarPath
    {
        get => VarA()?.Path ?? "";
        set => PatchVar(_ => new VariablesActionPatch { Path = value });
    }

    public string VarSeparator
    {
        get => VarA()?.Separator ?? "";
        set => PatchVar(_ => new VariablesActionPatch { Separator = value });
    }

    public string VarCustomSeparator
    {
        get => VarA()?.CustomSeparator ?? "";
        set => PatchVar(_ => new VariablesActionPatch { CustomSeparator = value });
    }

    public bool VarFirstLineIsHeader
    {
        get => VarA()?.FirstLineIsHeader ?? true;
        set => PatchVar(_ => new VariablesActionPatch { FirstLineIsHeader = value });
    }

    public string VarMinValue
    {
        get => VarA()?.MinValue ?? "";
        set => PatchVar(_ => new VariablesActionPatch { MinValue = value });
    }

    public string VarMaxValue
    {
        get => VarA()?.MaxValue ?? "";
        set => PatchVar(_ => new VariablesActionPatch { MaxValue = value });
    }

    public string VarDecimalPlaces
    {
        get => VarA()?.DecimalPlaces ?? "";
        set => PatchVar(_ => new VariablesActionPatch { DecimalPlaces = value });
    }

    public string VarCount
    {
        get => VarA()?.Count ?? "";
        set => PatchVar(_ => new VariablesActionPatch { Count = value });
    }

    public string VarItemIndex
    {
        get => VarA()?.ItemIndex ?? "";
        set => PatchVar(_ => new VariablesActionPatch { ItemIndex = value });
    }

    public string VarPropertyName
    {
        get => VarA()?.PropertyName ?? "";
        set => PatchVar(_ => new VariablesActionPatch { PropertyName = value });
    }

    public string VarSourceVariable
    {
        get => VarA()?.SourceVariable ?? "";
        set => PatchVar(_ => new VariablesActionPatch { SourceVariable = value });
    }

    public string VarJsonText
    {
        get => VarA()?.JsonText ?? "";
        set => PatchVar(_ => new VariablesActionPatch { JsonText = value });
    }

    public bool VarUseRegex
    {
        get => VarA()?.UseRegex ?? false;
        set => PatchVar(_ => new VariablesActionPatch { UseRegex = value });
    }

    public bool VarIgnoreCase
    {
        get => VarA()?.IgnoreCase ?? true;
        set => PatchVar(_ => new VariablesActionPatch { IgnoreCase = value });
    }

    public bool VarMatchCase
    {
        get => VarA()?.MatchCase ?? false;
        set => PatchVar(_ => new VariablesActionPatch { MatchCase = value });
    }

    public bool VarMatchEntireCell
    {
        get => VarA()?.MatchEntireCell ?? false;
        set => PatchVar(_ => new VariablesActionPatch { MatchEntireCell = value });
    }

    public bool VarRemoveAllMatches
    {
        get => VarA()?.RemoveAllMatches ?? false;
        set => PatchVar(_ => new VariablesActionPatch { RemoveAllMatches = value });
    }

    void RefreshVariablesInspector()
    {
        NotifyVariablesFieldProperties();
        NotifyVariablesInspectorSections();
    }

    void NotifyVariablesFieldProperties()
    {
        OnPropertyChanged(nameof(ShowActionVariables));
        foreach (var name in new[]
                 {
                     nameof(VarDataTableVariable), nameof(VarSecondDataTableVariable), nameof(VarListVariable),
                     nameof(VarSecondListVariable), nameof(VarOutputVariable), nameof(VarOutputVariable2),
                     nameof(VarValue), nameof(VarValues), nameof(VarItems), nameof(VarColumnName), nameof(VarColumnIndex),
                     nameof(VarRowIndex), nameof(VarFindText), nameof(VarReplaceText), nameof(VarMode),
                     nameof(VarSortColumn), nameof(VarSortOrder), nameof(VarSortColumn2), nameof(VarSortOrder2),
                     nameof(VarColumnWidths), nameof(VarFilterColumn), nameof(VarFilterOperator),
                     nameof(VarFilterValue), nameof(VarFilterLogic), nameof(VarFilterColumn2), nameof(VarFilterOperator2),
                     nameof(VarFilterValue2), nameof(VarFilterConditions), nameof(VarJoinColumn), nameof(VarJoinColumn2),
                     nameof(VarJoinOperation), nameof(VarJoinComparisonOperator),
                     nameof(VarPath), nameof(VarSeparator), nameof(VarCustomSeparator), nameof(VarFirstLineIsHeader),
                     nameof(VarMinValue), nameof(VarMaxValue), nameof(VarDecimalPlaces), nameof(VarCount),
                     nameof(VarItemIndex), nameof(VarPropertyName), nameof(VarSourceVariable), nameof(VarJsonText),
                     nameof(VarUseRegex), nameof(VarIgnoreCase), nameof(VarMatchCase),
                     nameof(VarMatchEntireCell), nameof(VarRemoveAllMatches),
                 })
            OnPropertyChanged(name);
    }
}
