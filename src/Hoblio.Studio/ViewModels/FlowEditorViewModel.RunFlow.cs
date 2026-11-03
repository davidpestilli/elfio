using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public ObservableCollection<string> EmbeddedSubflowIds { get; } = [];
    public ObservableCollection<string> RunFlowTargetIds { get; } = [];

    public bool ShowRunFlowSection => GetSelectedNode()?.Action is RunFlowAction;
    public bool ShowRunFlowOpenSubflow =>
        GetSelectedNode()?.Action is RunFlowAction rf
        && !string.IsNullOrWhiteSpace(rf.FlowId)
        && EmbeddedSubflowIds.Contains(rf.FlowId!);


    public string RunFlowId
    {
        get => GetRunFlow()?.FlowId ?? "";
        set => PatchRunFlow(rf => new RunFlowAction
        {
            FlowId = string.IsNullOrWhiteSpace(value) ? null : value.Trim(),
            Path = rf.Path,
            Inputs = rf.Inputs,
            Outputs = rf.Outputs,
            ShareBrowser = rf.ShareBrowser,
            VariableScope = rf.VariableScope,
        });
    }

    public string RunFlowPath
    {
        get => GetRunFlow()?.Path ?? "";
        set => PatchRunFlow(rf => new RunFlowAction
        {
            FlowId = rf.FlowId,
            Path = string.IsNullOrWhiteSpace(value) ? null : value.Trim(),
            Inputs = rf.Inputs,
            Outputs = rf.Outputs,
            ShareBrowser = rf.ShareBrowser,
            VariableScope = rf.VariableScope,
        });
    }

    public string RunFlowScopeMode
    {
        get => GetRunFlow()?.VariableScope == RunFlowVariableScope.Isolated ? "isolated" : "inherit";
        set => PatchRunFlow(rf => new RunFlowAction
        {
            FlowId = rf.FlowId,
            Path = rf.Path,
            Inputs = rf.Inputs,
            Outputs = rf.Outputs,
            ShareBrowser = rf.ShareBrowser,
            VariableScope = value == "isolated"
                ? RunFlowVariableScope.Isolated
                : RunFlowVariableScope.Inherit,
        });
    }

    public bool RunFlowShareBrowser
    {
        get => GetRunFlow()?.ShareBrowser is not false;
        set => PatchRunFlow(rf => new RunFlowAction
        {
            FlowId = rf.FlowId,
            Path = rf.Path,
            Inputs = rf.Inputs,
            Outputs = rf.Outputs,
            ShareBrowser = value,
            VariableScope = rf.VariableScope,
        });
    }

    public string RunFlowInputsText
    {
        get
        {
            var inputs = GetRunFlow()?.Inputs;
            if (inputs is null || inputs.Count == 0)
                return "";
            var sb = new StringBuilder();
            foreach (var (k, v) in inputs.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(k).Append('=').Append(v);
            }
            return sb.ToString();
        }
        set
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in value.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;
                var eq = trimmed.IndexOf('=');
                if (eq <= 0)
                    continue;
                var key = trimmed[..eq].Trim();
                var val = trimmed[(eq + 1)..].Trim();
                if (key.Length > 0)
                    dict[key] = val;
            }
            PatchRunFlow(rf => new RunFlowAction
            {
                FlowId = rf.FlowId,
                Path = rf.Path,
                Inputs = dict.Count > 0 ? dict : null,
                Outputs = rf.Outputs,
                ShareBrowser = rf.ShareBrowser,
                VariableScope = rf.VariableScope,
            });
        }
    }

    public string RunFlowOutputsText
    {
        get
        {
            var outs = GetRunFlow()?.Outputs;
            return outs is null || outs.Count == 0 ? "" : string.Join(", ", outs);
        }
        set => PatchRunFlow(rf => new RunFlowAction
        {
            FlowId = rf.FlowId,
            Path = rf.Path,
            Inputs = rf.Inputs,
            Outputs = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            ShareBrowser = rf.ShareBrowser,
            VariableScope = rf.VariableScope,
        });
    }

    public RelayCommand<string?> OpenEmbeddedSubflowCommand { get; private set; } = null!;

    void InitRunFlowCommands()
    {
        OpenEmbeddedSubflowCommand = new RelayCommand<string?>(id =>
        {
            if (string.IsNullOrWhiteSpace(id))
                return;
            SwitchEditor("subflow:" + id.Trim());
        });
        OpenSelectedRunFlowSubflowCommand = new RelayCommand(OpenSelectedRunFlowSubflow, () =>
            ShowRunFlowOpenSubflow && CanEditDiagram);
    }

    RunFlowAction? GetRunFlow() =>
        GetSelectedNode()?.Action as RunFlowAction;

    void PatchRunFlow(Func<RunFlowAction, RunFlowAction> patch)
    {
        var node = GetSelectedNode();
        if (node?.Action is not RunFlowAction current)
            return;
        node.Action = patch(current);
        MarkDirty();
        RefreshNodeSummary(node);
        RefreshAllInspector();
    }

    void RefreshRunFlowTargets()
    {
        EmbeddedSubflowIds.Clear();
        var graph = CurrentFileGraph();
        if (graph.Subflows is not null)
        {
            foreach (var id in graph.Subflows.Keys.OrderBy(k => k, StringComparer.Ordinal))
                EmbeddedSubflowIds.Add(id);
        }
        RunFlowTargetIds.Clear();
        foreach (var id in ManifestFlowIds)
            RunFlowTargetIds.Add(id);
        foreach (var id in EmbeddedSubflowIds)
        {
            if (!RunFlowTargetIds.Contains(id))
                RunFlowTargetIds.Add(id);
        }
        RebuildEmbeddedSubflowRows();
        ReloadFlowVariableProducers();
    }

    public void RenameEmbeddedSubflow(string oldId, string newId)
    {
        if (string.IsNullOrWhiteSpace(oldId) || string.IsNullOrWhiteSpace(newId) || !CanEditDiagram)
            return;
        oldId = oldId.Trim();
        newId = newId.Trim();
        PushHistory();
        PersistCanvasToActiveGraph();
        var graph = CurrentFileGraph();
        var result = SubflowDocumentOps.TryRename(graph, oldId, newId);
        if (!result.Ok)
        {
            SetStatus(result.Error ?? "Falha ao renomear subfluxo.", isError: true);
            return;
        }
        if (string.Equals(_activeSubflowId, oldId, StringComparison.Ordinal))
            ActiveSubflowId = newId;
        RebuildEditorTabs();
        RebuildCanvas();
        RefreshRunFlowTargets();
        MarkDirty();
        var extra = result.UpdatedRunFlowNodes > 0
            ? $" · {result.UpdatedRunFlowNodes} nó(s) run_flow atualizado(s)"
            : "";
        _activeSubflowRenameDraft = newId;
        OnPropertyChanged(nameof(ActiveSubflowRenameDraft));
        SetStatus($"Subfluxo {oldId} → {newId}{extra}");
    }

    public void DeleteEmbeddedSubflow(string? subflowId)
    {
        if (string.IsNullOrWhiteSpace(subflowId) || !CanEditDiagram)
            return;
        var id = subflowId.Trim();
        if (MessageBox.Show(
                $"Excluir subfluxo embutido \"{id}\"? Nós run_flow que apontam para ele perderão flow_id.",
                "Excluir subfluxo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        PushHistory();
        PersistCanvasToActiveGraph();
        var graph = CurrentFileGraph();
        var result = SubflowDocumentOps.TryDelete(graph, id);
        if (!result.Ok)
        {
            SetStatus(result.Error ?? "Falha ao excluir subfluxo.", isError: true);
            return;
        }
        if (string.Equals(_activeSubflowId, id, StringComparison.Ordinal))
            ActiveSubflowId = null;
        RebuildEditorTabs();
        RebuildCanvas();
        RefreshRunFlowTargets();
        MarkDirty();
        var extra = result.ClearedRunFlowNodes > 0
            ? $" · {result.ClearedRunFlowNodes} referência(s) run_flow limpa(s)"
            : "";
        SetStatus($"Subfluxo \"{id}\" removido{extra}");
    }

    void OpenSelectedRunFlowSubflow()
    {
        if (GetRunFlow()?.FlowId is not { } flowId || string.IsNullOrWhiteSpace(flowId))
            return;
        SwitchEditor("subflow:" + flowId.Trim());
    }

    public RelayCommand OpenSelectedRunFlowSubflowCommand { get; private set; } = null!;
}
