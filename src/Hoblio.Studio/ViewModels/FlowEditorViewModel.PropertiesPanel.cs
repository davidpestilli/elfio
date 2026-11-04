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
        ActiveSubflowId is null
            ? ""
            : $"Subfluxo {ActiveSubflowId} — valores iniciais em subflows.{ActiveSubflowId}.variables.";

    public string ActiveSubflowDefinedHint =>
        ActiveSubflowId is null ? "" : $"{ActiveFlowPath} → subflows.{ActiveSubflowId}";

    public bool ShowEmptyFlowProducers => NoSelectedNode && FlowVariableProducers.Count == 0;

    public int EmbeddedSubflowCount => EmbeddedSubflowRows.Count;

    public int FlowVariableProducerCount => FlowVariableProducers.Count;

    public ObservableCollection<FlowVariableProducer> FilteredFlowVariableProducers { get; } = [];

    public bool ShowFlowVariableProducerSearch => FlowVariableProducers.Count > 6;

    public bool ShowEmptyFilteredFlowProducers =>
        NoSelectedNode && FlowVariableProducers.Count > 0 && FilteredFlowVariableProducers.Count == 0;

    string _flowVariableProducerFilter = "";
    public string FlowVariableProducerFilter
    {
        get => _flowVariableProducerFilter;
        set
        {
            if (_flowVariableProducerFilter == value)
                return;
            _flowVariableProducerFilter = value;
            OnPropertyChanged();
            RebuildFilteredFlowVariableProducers();
            OnPropertyChanged(nameof(ShowEmptyFilteredFlowProducers));
        }
    }

    public bool ShowNodeLogicSection =>
        ShowIfSection || ShowWhileSection || ShowRepeatSection || ShowForEachSection || ShowNextExitLoopHint;

    public string NodeActionSectionTitle
    {
        get
        {
            if (!ShowActionSection)
                return "Ação";
            var type = ActionType;
            return string.IsNullOrWhiteSpace(type) ? "Ação" : $"Ação · {type}";
        }
    }

    public string VariablesSectionTitle => "Valores iniciais";

    public string VariablesSectionHint => ActiveSubflowId is not null
        ? "Padrões deste subfluxo (gravados em subflows no JSON)."
        : "Variáveis iniciais deste arquivo de fluxo (orquestrador de entrada usa flow.json).";

    public string PropertiesPanelSubtitle
    {
        get
        {
            if (SelectedConnection is not null)
                return "Ligação selecionada";
            if (HasSelectedNode)
            {
                var kind = SelectedNodeKindDisplay;
                var label = NodeLabelEdit.Trim();
                if (!string.IsNullOrEmpty(label) && !string.Equals(label, kind, StringComparison.OrdinalIgnoreCase))
                    return $"{kind} · {label}";
                return string.IsNullOrEmpty(kind) ? "Nó selecionado" : kind;
            }
            if (ActiveSubflowId is not null)
                return $"Subfluxo · {ActiveSubflowId}";
            return "Fluxo · valores iniciais e subfluxos";
        }
    }

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
        OnPropertyChanged(nameof(EmbeddedSubflowCount));
        RefreshPropertiesPanelContext();
    }

    void RefreshPropertiesPanelVisibility()
    {
        OnPropertyChanged(nameof(ShowCanvasProperties));
        OnPropertyChanged(nameof(ShowNodeProperties));
        OnPropertyChanged(nameof(ShowActiveSubflowManage));
        OnPropertyChanged(nameof(ShowEmbeddedSubflowsList));
        OnPropertyChanged(nameof(ShowSubflowContextBanner));
        OnPropertyChanged(nameof(VariablesSectionHint));
        OnPropertyChanged(nameof(VariablesSectionTitle));
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
        OnPropertyChanged(nameof(EmbeddedSubflowCount));
        OnPropertyChanged(nameof(FlowVariableProducerCount));
        OnPropertyChanged(nameof(ShowFlowVariableProducerSearch));
        OnPropertyChanged(nameof(ShowNodeLogicSection));
        OnPropertyChanged(nameof(NodeActionSectionTitle));
        OnPropertyChanged(nameof(ShowEmptyFilteredFlowProducers));
        RefreshPropertiesPanelContext();
        ApplyActiveSubflowRenameCommand?.RaiseCanExecuteChanged();
        DeleteActiveSubflowCommand?.RaiseCanExecuteChanged();
    }

    void RefreshPropertiesPanelContext() => OnPropertyChanged(nameof(PropertiesPanelSubtitle));

    void RebuildFilteredFlowVariableProducers()
    {
        FilteredFlowVariableProducers.Clear();
        var q = FlowVariableProducerFilter.Trim();
        foreach (var p in FlowVariableProducers)
        {
            if (string.IsNullOrEmpty(q)
                || p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || p.Source.Contains(q, StringComparison.OrdinalIgnoreCase)
                || p.NodeLabel.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                FilteredFlowVariableProducers.Add(p);
            }
        }
        OnPropertyChanged(nameof(ShowEmptyFilteredFlowProducers));
    }
}
