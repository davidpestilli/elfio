using System.Windows;
using Hoblio.Model;
using Hoblio.Studio;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    int _pasteGeneration;

    public RelayCommand CopySelectionCommand { get; private set; } = null!;
    public RelayCommand CutSelectionCommand { get; private set; } = null!;
    public RelayCommand PasteCommand { get; private set; } = null!;

    public bool CanCopySelection => CanEditDiagram && SelectedNodes.Count > 0;

    public bool CanPasteFromClipboard => CanEditDiagram && ClipboardContainsDiagram();

    void InitClipboardCommands()
    {
        CopySelectionCommand = new RelayCommand(CopySelection, () => CanCopySelection);
        CutSelectionCommand = new RelayCommand(CutSelection, () => CanCopySelection);
        PasteCommand = new RelayCommand(PasteFromClipboard, () => CanPasteFromClipboard);
    }

    void RaiseClipboardCommands()
    {
        CopySelectionCommand.RaiseCanExecuteChanged();
        CutSelectionCommand.RaiseCanExecuteChanged();
        PasteCommand.RaiseCanExecuteChanged();
    }

    static bool ClipboardContainsDiagram()
    {
        if (!Clipboard.ContainsText())
            return false;
        return DiagramClipboard.TryDeserialize(Clipboard.GetText(), out _);
    }

    void CopySelection()
    {
        if (!CanCopySelection)
            return;
        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();
        var ids = SelectedNodes.Select(n => n.Id).ToList();
        var payload = DiagramClipboard.ExtractSelection(nodes, edges, ids);
        if (payload.Nodes.Count == 0)
            return;
        Clipboard.SetText(DiagramClipboard.Serialize(payload));
        SetStatus($"{payload.Nodes.Count} nó(s) copiado(s) — Ctrl+V para colar.");
        RaiseClipboardCommands();
    }

    void CutSelection()
    {
        CopySelection();
        DeleteSelectedNodes();
        RaiseClipboardCommands();
    }

    void PasteFromClipboard()
    {
        if (!CanEditDiagram || !DiagramClipboard.TryDeserialize(Clipboard.GetText(), out var payload) || payload is null)
            return;

        PushHistory();
        PersistCanvasToActiveGraph();
        var (nodes, edges, _) = ActiveGraphSlice();
        var hasStart = nodes.Any(n => n.Kind == NodeKind.Start);
        _pasteGeneration++;
        var offset = 32.0 * (_pasteGeneration % 8);
        var (pastedNodes, pastedEdges) = DiagramClipboard.PasteWithNewIds(payload, hasStart, offset, offset);
        if (pastedNodes.Count == 0)
        {
            SetStatus("Nada para colar (Start duplicado ignorado).", isError: true);
            return;
        }

        nodes.AddRange(pastedNodes);
        edges.AddRange(pastedEdges);
        CommitGraphSlice(nodes, edges);
        var vms = pastedNodes.Select(n => Nodes.FirstOrDefault(v => v.Id == n.Id)).Where(v => v is not null).Cast<FlowNodeViewModel>().ToList();
        _syncingCanvasSelection = true;
        try
        {
            SelectedNodes.Clear();
            foreach (var vm in vms)
                SelectedNodes.Add(vm);
        }
        finally
        {
            _syncingCanvasSelection = false;
        }
        OnPropertyChanged(nameof(SelectedNodeCount));
        OnPropertyChanged(nameof(HasMultiSelection));
        SyncPrimaryFromSelection();
        PushDiagramSelection(vms);
        SetStatus($"{pastedNodes.Count} nó(s) colado(s).");
    }
}
