namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public RelayCommand<RunLogLine?> FocusLogLineNodeCommand { get; private set; } = null!;

    void InitRunLogCommands()
    {
        FocusLogLineNodeCommand = new RelayCommand<RunLogLine?>(
            FocusLogLineNode,
            line => line is { CanFocusNode: true });
    }

    void FocusLogLineNode(RunLogLine? line)
    {
        if (line?.NodeId is not { } id)
            return;
        var vm = Nodes.FirstOrDefault(n => n.Id == id);
        if (vm is null)
        {
            SetStatus($"Nó «{id}» não está visível neste diagrama (pode estar em outro subfluxo).", isError: true);
            return;
        }

        SelectSingleNode(vm);
        FlowCanvas?.FlashNode(id);
        SetStatus($"Nó «{id}» destacado no canvas.");
    }
}
