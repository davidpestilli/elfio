using System.Collections.ObjectModel;
using Hoblio.Model;
using Hoblio.Studio;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public ObservableCollection<EmbeddedSubflowRowViewModel> EmbeddedSubflowRows { get; } = [];

    public bool ShowCanvasProperties => NoSelectedNode;
    public bool ShowNodeProperties => HasSelectedNode;

    public bool ShowActiveSubflowManage => NoSelectedNode && ActiveSubflowId is not null;
    public bool ShowEmbeddedSubflowsList => NoSelectedNode && ActiveSubflowId is null && EmbeddedSubflowIds.Count > 0;
    public bool ShowSubflowContextBanner => HasSelectedNode && ActiveSubflowId is not null;

    public string SubflowContextBannerText =>
        ActiveSubflowId is null ? "" : $"Subflow {ActiveSubflowId} — seeds live under subflows.{ActiveSubflowId}.variables.";

    public string ActiveSubflowDefinedHint =>
        ActiveSubflowId is null ? "" : $"Defined in {ActiveFlowPath} → subflows.{ActiveSubflowId}";

    public bool ShowEmptyFlowProducers => NoSelectedNode && FlowVariableProducers.Count == 0;

    public string VariablesSectionTitle => "Project variables";

    public string VariablesSectionHint => ActiveSubflowId is not null
        ? "Seeds and defaults for this subflow only (saved under subflows in JSON)."
        : "Initial run variables for this flow file (entry orchestrator uses flow.json).";

    public string SelectedNodeKindDisplay => GetSelectedNode()?.Kind.ToString() ?? "";

    public bool IsStartNodeSelected => GetSelectedNode()?.Kind == NodeKind.Start;

    public bool ShowLogFields => GetSelectedNode()?.Action is LogAction;
    public bool ShowLastRunValues =>
        HasSelectedNode && GetSelectedNode()?.Kind == NodeKind.Action && !string.IsNullOrWhiteSpace(LastRunVariablesText);

    public bool ShowOnErrorSection => GetSelectedNode()?.Kind == NodeKind.Action;

    public bool ShowDeleteNodeButton
    {
        get
        {
            var node = GetSelectedNode();
            return node is not null && node.Kind is not (NodeKind.Start or NodeKind.End);
        }
    }

    public bool ShowNextExitLoopHint =>
        GetSelectedNode()?.Kind is NodeKind.NextLoop or NodeKind.ExitLoop;

    public string OnFailureMode
    {
        get => GetSelectedNode()?.ErrorHandling?.OnFailure == OnFailure.Continue ? "continue" : "stop";
        set => PatchErrorHandling(eh => eh.OnFailure = value == "continue" ? OnFailure.Continue : OnFailure.Stop);
    }

    public string ActiveSubflowRenameDraft
    {
        get => _activeSubflowRenameDraft ?? ActiveSubflowId ?? "";
        set
        {
            _activeSubflowRenameDraft = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActiveSubflowCanRename));
            OnPropertyChanged(nameof(ActiveSubflowRenameError));
        }
    }

    string? _activeSubflowRenameDraft;

    public string? ActiveSubflowRenameError
    {
        get
        {
            if (ActiveSubflowId is null || ActiveSubflowRenameDraft == ActiveSubflowId)
                return null;
            if (!SubflowDocumentOps.IsValidSubflowId(ActiveSubflowRenameDraft))
                return "Id inválido (letra/_ inicial, depois alfanumérico/_).";
            if (EmbeddedSubflowIds.Any(x => x == ActiveSubflowRenameDraft && x != ActiveSubflowId))
                return $"Subfluxo \"{ActiveSubflowRenameDraft}\" já existe.";
            return null;
        }
    }

    public bool ActiveSubflowCanRename =>
        ActiveSubflowId is not null
        && ActiveSubflowRenameDraft != ActiveSubflowId
        && ActiveSubflowRenameError is null;

    public RelayCommand ApplyActiveSubflowRenameCommand { get; private set; } = null!;
    public RelayCommand DeleteActiveSubflowCommand { get; private set; } = null!;

    void InitPropertiesPanelCommands()
    {
        ApplyActiveSubflowRenameCommand = new RelayCommand(() =>
        {
            if (ActiveSubflowId is not null)
                RenameEmbeddedSubflow(ActiveSubflowId, ActiveSubflowRenameDraft);
        }, () => ActiveSubflowCanRename && CanEditDiagram);
        DeleteActiveSubflowCommand = new RelayCommand(() =>
        {
            if (ActiveSubflowId is not null)
                DeleteEmbeddedSubflow(ActiveSubflowId);
        }, () => ActiveSubflowId is not null && CanEditDiagram);
    }

    void RebuildEmbeddedSubflowRows()
    {
        EmbeddedSubflowRows.Clear();
        foreach (var id in EmbeddedSubflowIds)
        {
            var row = new EmbeddedSubflowRowViewModel(id, this);
            EmbeddedSubflowRows.Add(row);
        }
        _activeSubflowRenameDraft = ActiveSubflowId;
        OnPropertyChanged(nameof(ActiveSubflowRenameDraft));
        OnPropertyChanged(nameof(ShowActiveSubflowManage));
        OnPropertyChanged(nameof(ShowEmbeddedSubflowsList));
        OnPropertyChanged(nameof(ShowCanvasProperties));
        OnPropertyChanged(nameof(ShowNodeProperties));
        OnPropertyChanged(nameof(VariablesSectionHint));
        OnPropertyChanged(nameof(ShowSubflowContextBanner));
    }

    void RefreshPropertiesPanelVisibility()
    {
        OnPropertyChanged(nameof(ShowCanvasProperties));
        OnPropertyChanged(nameof(ShowNodeProperties));
        OnPropertyChanged(nameof(ShowActiveSubflowManage));
        OnPropertyChanged(nameof(ShowEmbeddedSubflowsList));
        OnPropertyChanged(nameof(ShowSubflowContextBanner));
        OnPropertyChanged(nameof(VariablesSectionHint));
        OnPropertyChanged(nameof(SelectedNodeKindDisplay));
        OnPropertyChanged(nameof(IsStartNodeSelected));
        OnPropertyChanged(nameof(ShowLogFields));
        OnPropertyChanged(nameof(ShowLastRunValues));
        OnPropertyChanged(nameof(ShowOnErrorSection));
        OnPropertyChanged(nameof(ShowNextExitLoopHint));
        OnPropertyChanged(nameof(OnFailureMode));
        OnPropertyChanged(nameof(SubflowContextBannerText));
        OnPropertyChanged(nameof(ActiveSubflowDefinedHint));
        OnPropertyChanged(nameof(ShowEmptyFlowProducers));
        OnPropertyChanged(nameof(ShowDeleteNodeButton));
        ApplyActiveSubflowRenameCommand?.RaiseCanExecuteChanged();
        DeleteActiveSubflowCommand?.RaiseCanExecuteChanged();
    }
}
