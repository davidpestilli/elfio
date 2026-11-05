using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    string? SelectedVariablesType => VarA()?.Operation;

    public bool ShowVarSectionDatatable => VariablesActionUi.UsesDatatable(SelectedVariablesType);
    public bool ShowVarSectionSecondTable => VariablesActionUi.UsesSecondDatatable(SelectedVariablesType);
    public bool ShowVarSectionList => VariablesActionUi.UsesList(SelectedVariablesType);
    public bool ShowVarSectionSecondList => VariablesActionUi.UsesSecondList(SelectedVariablesType);
    public bool ShowVarSectionRowValues => VariablesActionUi.UsesRowValues(SelectedVariablesType);
    public bool ShowVarSectionCellCoords => VariablesActionUi.UsesCellCoords(SelectedVariablesType);
    public bool ShowVarSectionFindReplace => VariablesActionUi.UsesFindReplace(SelectedVariablesType);
    public bool ShowVarSectionFilterTable => VariablesActionUi.UsesFilterTable(SelectedVariablesType);
    public bool ShowVarSectionFilterList => VariablesActionUi.UsesFilterList(SelectedVariablesType);
    public bool ShowVarSectionSortTable => VariablesActionUi.UsesSortTable(SelectedVariablesType);
    public bool ShowVarSectionJoinMerge => VariablesActionUi.UsesJoinMerge(SelectedVariablesType);
    public bool ShowVarSectionCsv => VariablesActionUi.UsesCsv(SelectedVariablesType);
    public bool ShowVarSectionListContent => VariablesActionUi.UsesListContent(SelectedVariablesType);
    public bool ShowVarSectionListItemOps => VariablesActionUi.UsesListItemOps(SelectedVariablesType);
    public bool ShowVarSectionNumbers => VariablesActionUi.UsesNumbers(SelectedVariablesType);
    public bool ShowVarSectionJson => VariablesActionUi.UsesJson(SelectedVariablesType);
    public bool ShowVarSectionOutput => VariablesActionUi.UsesOutputVariable(SelectedVariablesType);

    void NotifyVariablesInspectorSections()
    {
        foreach (var name in new[]
                 {
                     nameof(ShowVarSectionDatatable), nameof(ShowVarSectionSecondTable), nameof(ShowVarSectionList),
                     nameof(ShowVarSectionSecondList), nameof(ShowVarSectionRowValues), nameof(ShowVarSectionCellCoords),
                     nameof(ShowVarSectionFindReplace), nameof(ShowVarSectionFilterTable), nameof(ShowVarSectionFilterList),
                     nameof(ShowVarSectionSortTable),
                     nameof(ShowVarSectionJoinMerge), nameof(ShowVarSectionCsv), nameof(ShowVarSectionListContent),
                     nameof(ShowVarSectionListItemOps), nameof(ShowVarSectionNumbers), nameof(ShowVarSectionJson),
                     nameof(ShowVarSectionOutput),
                 })
            OnPropertyChanged(name);
    }
}
