using System.ComponentModel;
using System.Runtime.CompilerServices;
using Hoblio.Studio;

namespace Hoblio.Studio.ViewModels;

public sealed class EmbeddedSubflowRowViewModel : INotifyPropertyChanged
{
    readonly FlowEditorViewModel _host;

    public EmbeddedSubflowRowViewModel(string id, FlowEditorViewModel host)
    {
        Id = id;
        _host = host;
        _renameDraft = id;
        ApplyRenameCommand = new RelayCommand(() =>
        {
            _host.RenameEmbeddedSubflow(Id, RenameDraft);
            ShowRenameEditor = false;
        }, () => CanApplyRename);
        DeleteCommand = new RelayCommand(() => _host.DeleteEmbeddedSubflow(Id), () => _host.CanEditDiagram);
        BeginRenameCommand = new RelayCommand(() => ShowRenameEditor = true);
        CancelRenameCommand = new RelayCommand(() =>
        {
            RenameDraft = Id;
            ShowRenameEditor = false;
        });
    }

    public string Id { get; }

    string _renameDraft;
    public string RenameDraft
    {
        get => _renameDraft;
        set
        {
            if (_renameDraft == value)
                return;
            _renameDraft = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanApplyRename));
            OnPropertyChanged(nameof(RenameError));
            ApplyRenameCommand.RaiseCanExecuteChanged();
        }
    }

    public string? RenameError
    {
        get
        {
            if (RenameDraft == Id)
                return null;
            if (!SubflowDocumentOps.IsValidSubflowId(RenameDraft))
                return "Id inválido (letra/_ inicial, depois alfanumérico/_).";
            if (_host.EmbeddedSubflowIds.Any(x => x == RenameDraft && x != Id))
                return $"Subfluxo \"{RenameDraft}\" já existe.";
            return null;
        }
    }

    public bool CanApplyRename => RenameDraft != Id && RenameError is null;

    bool _showRenameEditor;
    public bool ShowRenameEditor
    {
        get => _showRenameEditor;
        set
        {
            if (_showRenameEditor == value)
                return;
            _showRenameEditor = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand<string?> OpenCommand => _host.OpenEmbeddedSubflowCommand;
    public RelayCommand ApplyRenameCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand BeginRenameCommand { get; }
    public RelayCommand CancelRenameCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
