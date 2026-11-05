using System.Collections.ObjectModel;
using Hoblio.Model;

namespace Hoblio.Studio.ViewModels;

public sealed partial class FlowEditorViewModel
{
    public ObservableCollection<string> TemplateVariableNames { get; } = [];

    public RelayCommand<string> InsertTemplateTokenCommand { get; private set; } = null!;

    void InitTemplateVariableCommands() =>
        InsertTemplateTokenCommand = new RelayCommand<string>(InsertTemplateToken, _ => CanEditDiagram);

    void RebuildTemplateVariableNames()
    {
        TemplateVariableNames.Clear();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in ProjectScalars)
        {
            var n = s.Name.Trim();
            if (n.Length > 0 && seen.Add(n))
                TemplateVariableNames.Add(n);
        }

        var (nodes, _, _) = ActiveGraphSlice();
        foreach (var p in Hoblio.Studio.FlowVariableProducers.Collect(nodes))
        {
            if (seen.Add(p.Name))
                TemplateVariableNames.Add(p.Name);
        }

        OnPropertyChanged(nameof(ShowTemplateVariablePicker));
    }

    public bool ShowTemplateVariablePicker => TemplateVariableNames.Count > 0;

    void InsertTemplateToken(string? parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter))
            return;
        var sep = parameter.IndexOf('|');
        if (sep <= 0 || sep >= parameter.Length - 1)
            return;
        var field = parameter[..sep];
        var varName = parameter[(sep + 1)..];
        var token = "{" + varName + "}";

        switch (field)
        {
            case "LogMessage":
                LogMessageEdit = LogMessageEdit + token;
                break;
            case "IfLeft":
                IfLeft = IfLeft + token;
                break;
            case "IfRight":
                IfRight = IfRight + token;
                break;
            case "SwitchExpression":
                SwitchExpression = SwitchExpression + token;
                break;
            case "CaseValue":
                CaseValue = CaseValue + token;
                break;
            case "WhileLeft":
                WhileLeft = WhileLeft + token;
                break;
            case "WhileRight":
                WhileRight = WhileRight + token;
                break;
            case "SetVariableValue":
                SetVariableValue = SetVariableValue + token;
                break;
            case "FileWriteContent":
                FileWriteContent = FileWriteContent + token;
                break;
            case "FileAppendContent":
                FileAppendContent = FileAppendContent + token;
                break;
            case "ListAppendItem":
                ListAppendItem = ListAppendItem + token;
                break;
            case "VarFilterValue":
                VarFilterValue = VarFilterValue + token;
                break;
            case "VarValues":
                VarValues = VarValues + token;
                break;
            case "VarValue":
                VarValue = VarValue + token;
                break;
            case "VarFindText":
                VarFindText = VarFindText + token;
                break;
            case "VarReplaceText":
                VarReplaceText = VarReplaceText + token;
                break;
            case "VarFilterValue2":
                VarFilterValue2 = VarFilterValue2 + token;
                break;
            case "VarPath":
                VarPath = VarPath + token;
                break;
            case "VarJoinColumn":
                VarJoinColumn = VarJoinColumn + token;
                break;
            case "VarJsonText":
                VarJsonText = VarJsonText + token;
                break;
            case "VarItems":
                VarItems = VarItems + token;
                break;
            case "VarSourceVariable":
                VarSourceVariable = VarSourceVariable + token;
                break;
            case "VarMinValue":
                VarMinValue = VarMinValue + token;
                break;
            case "VarMaxValue":
                VarMaxValue = VarMaxValue + token;
                break;
            case "ShowMessageText":
                ShowMessageText = ShowMessageText + token;
                break;
            case "UserInputMessage":
                UserInputMessage = UserInputMessage + token;
                break;
            case "SelectFromListMessage":
                SelectFromListMessage = SelectFromListMessage + token;
                break;
            case "SelectFromListItems":
                SelectFromListItems = SelectFromListItems + token;
                break;
            case "WebText":
                WebText = WebText + token;
                break;
            case "WebUrl":
                WebUrl = WebUrl + token;
                break;
            case "RunFlowInputsText":
                RunFlowInputsText = RunFlowInputsText + token;
                break;
            default:
                return;
        }

        SetStatus($"Inserido {token}.");
    }
}
