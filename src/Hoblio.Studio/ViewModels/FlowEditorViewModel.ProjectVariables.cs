using System.Collections.ObjectModel;
using System.Text.Json;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    bool _syncingProjectVariablesUi;

    public ObservableCollection<ScalarVariableRow> ProjectScalars { get; } = [];
    public ObservableCollection<ListVariableRow> ProjectLists { get; } = [];
    public ObservableCollection<string> ProjectListNames { get; } = [];
    public ObservableCollection<FlowVariableProducer> FlowVariableProducers { get; } = [];

    public string VariablesAdvancedJson
    {
        get => VariablesJson;
        set => VariablesJson = value;
    }

    public RelayCommand AddProjectScalarCommand { get; private set; } = null!;
    public RelayCommand AddProjectListCommand { get; private set; } = null!;
    public RelayCommand<string?> SelectFlowProducerNodeCommand { get; private set; } = null!;

    void InitProjectVariablesCommands()
    {
        AddProjectScalarCommand = new RelayCommand(AddProjectScalar, () => CanEditDiagram);
        AddProjectListCommand = new RelayCommand(AddProjectList, () => CanEditDiagram);
        SelectFlowProducerNodeCommand = new RelayCommand<string?>(id =>
        {
            if (string.IsNullOrWhiteSpace(id))
                return;
            var vm = Nodes.FirstOrDefault(n => n.Id == id);
            SelectSingleNode(vm);
        });
    }

    void ReloadProjectVariablesUi()
    {
        _syncingProjectVariablesUi = true;
        try
        {
            ProjectScalars.Clear();
            ProjectLists.Clear();
            Dictionary<string, JsonElement> vars;
            try
            {
                vars = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(VariablesJson)
                       ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            }
            catch
            {
                vars = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            }
            var (scalars, lists, _) = ProjectVariablesEditor.Partition(vars);
            foreach (var s in scalars)
                ProjectScalars.Add(s);
            foreach (var l in lists)
                ProjectLists.Add(l);
            RebuildProjectListNames();
        }
        finally
        {
            _syncingProjectVariablesUi = false;
        }
    }

    void RebuildProjectListNames()
    {
        ProjectListNames.Clear();
        foreach (var l in ProjectLists)
        {
            if (!string.IsNullOrWhiteSpace(l.Name))
                ProjectListNames.Add(l.Name);
        }
        OnPropertyChanged(nameof(ActionTemplateHint));
        RebuildTemplateVariableNames();
    }

    void ReloadFlowVariableProducers()
    {
        FlowVariableProducers.Clear();
        var (nodes, _, _) = ActiveGraphSlice();
        foreach (var p in Hoblio.Studio.FlowVariableProducers.Collect(nodes))
            FlowVariableProducers.Add(p);
        OnPropertyChanged(nameof(ShowEmptyFlowProducers));
        OnPropertyChanged(nameof(FlowVariableProducerCount));
        OnPropertyChanged(nameof(ExpandFlowVariablesSection));
        OnPropertyChanged(nameof(ShowFlowVariableProducerSearch));
        RebuildFilteredFlowVariableProducers();
        RebuildTemplateVariableNames();
    }

    void CommitProjectVariablesFromUi()
    {
        if (_syncingProjectVariablesUi)
            return;
        var snap = CurrentFileSnapshot();
        var merged = ProjectVariablesEditor.Build(ProjectScalars, ProjectLists, snap.Variables);
        var json = ProjectVariablesEditor.ToPrettyJson(merged);
        if (json == VariablesJson)
            return;
        _variablesJson = json;
        snap.Variables = merged;
        if (string.Equals(_activeFlowPath, _entryFlowPath, StringComparison.OrdinalIgnoreCase))
            _project.Variables = merged;
        OnPropertyChanged(nameof(VariablesJson));
        OnPropertyChanged(nameof(VariablesAdvancedJson));
        MarkDirty();
        RebuildProjectListNames();
    }

    void AddProjectScalar()
    {
        var taken = new HashSet<string>(ProjectScalars.Select(s => s.Name), StringComparer.Ordinal);
        for (var i = 1; i < 100; i++)
        {
            var name = i == 1 ? "var1" : $"var{i}";
            if (!taken.Contains(name))
            {
                ProjectScalars.Add(new ScalarVariableRow { Name = name, Kind = ScalarVariableKind.Text });
                CommitProjectVariablesFromUi();
                return;
            }
        }
    }

    void AddProjectList()
    {
        var taken = new HashSet<string>(ProjectLists.Select(s => s.Name), StringComparer.Ordinal);
        for (var i = 1; i < 100; i++)
        {
            var name = i == 1 ? "items1" : $"items{i}";
            if (!taken.Contains(name))
            {
                var row = new ListVariableRow { Name = name };
                row.Items.Add("");
                ProjectLists.Add(row);
                CommitProjectVariablesFromUi();
                return;
            }
        }
    }

    public string? ProjectVariablesValidationError
    {
        get
        {
            foreach (var s in ProjectScalars)
            {
                var n = s.Name.Trim();
                if (n.Length == 0)
                    continue;
                if (!ProjectVariablesEditor.IsValidVariableName(n))
                    return $"Variável de projeto \"{n}\" tem nome inválido.";
            }
            foreach (var l in ProjectLists)
            {
                var n = l.Name.Trim();
                if (n.Length == 0)
                    continue;
                if (!ProjectVariablesEditor.IsValidVariableName(n))
                    return $"Lista \"{n}\" tem nome inválido.";
            }
            return null;
        }
    }

    public bool HasProjectVariablesValidationError => !string.IsNullOrEmpty(ProjectVariablesValidationError);

    public void NotifyProjectVariableRowChanged()
    {
        OnPropertyChanged(nameof(ProjectVariablesValidationError));
        OnPropertyChanged(nameof(HasProjectVariablesValidationError));
        if (ProjectVariablesValidationError is not null)
        {
            SetStatus(ProjectVariablesValidationError, isError: true);
            return;
        }
        CommitProjectVariablesFromUi();
    }
}
