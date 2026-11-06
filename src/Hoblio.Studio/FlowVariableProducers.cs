using Hoblio.Model;

namespace Hoblio.Studio;

public sealed record FlowVariableProducer(string Name, string NodeId, string NodeLabel, string Source);

public static class FlowVariableProducers
{
    public static IReadOnlyList<FlowVariableProducer> Collect(IEnumerable<Node> nodes)
    {
        var list = new List<FlowVariableProducer>();
        foreach (var node in nodes)
        {
            var title = string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label.Trim();
            if (node.Action is WebAutomationAction wa && !string.IsNullOrWhiteSpace(wa.Variable))
                list.Add(new(wa.Variable.Trim(), node.Id, title, WebAutomationActionTypes.ModuleActionId(wa.Type)));
            if (node.Action is HttpRequestAction hr)
            {
                if (!string.IsNullOrWhiteSpace(hr.ResponseBodyVariable))
                    list.Add(new(hr.ResponseBodyVariable.Trim(), node.Id, title, "HttpRequest (body)"));
                if (!string.IsNullOrWhiteSpace(hr.ResponseHeadersVariable))
                    list.Add(new(hr.ResponseHeadersVariable.Trim(), node.Id, title, "HttpRequest (headers)"));
                if (!string.IsNullOrWhiteSpace(hr.StatusCodeVariable))
                    list.Add(new(hr.StatusCodeVariable.Trim(), node.Id, title, "HttpRequest (status)"));
            }
            if (node.Action is LlmCompletionAction llm && !string.IsNullOrWhiteSpace(llm.ResponseBodyVariable))
                list.Add(new(llm.ResponseBodyVariable.Trim(), node.Id, title, "LLM completion"));
            if (node.Action is SetVariableAction sv && !string.IsNullOrWhiteSpace(sv.Variable))
                list.Add(new(sv.Variable.Trim(), node.Id, title, "SetVariable"));
            if (node.Action is FileReadAction fr && !string.IsNullOrWhiteSpace(fr.Variable))
                list.Add(new(fr.Variable.Trim(), node.Id, title, "FileRead"));
            if (node.Action is PdfAction pdf && !string.IsNullOrWhiteSpace(pdf.OutputVariable))
                list.Add(new(pdf.OutputVariable.Trim(), node.Id, title, PdfActionTypes.ModuleActionId(pdf.Type)));
            if (node.Action is ShowGetUserInputAction ui)
            {
                if (!string.IsNullOrWhiteSpace(ui.UserInputVariable))
                    list.Add(new(ui.UserInputVariable.Trim(), node.Id, title, "ShowGetUserInput (UserInput)"));
                if (!string.IsNullOrWhiteSpace(ui.ButtonPressedVariable))
                    list.Add(new(ui.ButtonPressedVariable.Trim(), node.Id, title, "ShowGetUserInput (ButtonPressed)"));
            }
            if (node.Action is ShowSelectFromListAction sl)
            {
                if (!string.IsNullOrWhiteSpace(sl.SelectedItemVariable))
                    list.Add(new(sl.SelectedItemVariable.Trim(), node.Id, title, "ShowSelectFromList (SelectedItem)"));
                if (!string.IsNullOrWhiteSpace(sl.SelectedIndexVariable))
                    list.Add(new(sl.SelectedIndexVariable.Trim(), node.Id, title, "ShowSelectFromList (SelectedIndex)"));
                if (!string.IsNullOrWhiteSpace(sl.ButtonPressedVariable))
                    list.Add(new(sl.ButtonPressedVariable.Trim(), node.Id, title, "ShowSelectFromList (ButtonPressed)"));
            }
            if (node.Kind == NodeKind.ForEach && node.Control is ForEachControlKind fe && !string.IsNullOrWhiteSpace(fe.ItemVariable))
                list.Add(new(fe.ItemVariable.Trim(), node.Id, title, "for_each item"));
            if (node.Kind == NodeKind.Repeat && node.Control is RepeatControlKind rp && !string.IsNullOrWhiteSpace(rp.IndexVariable))
                list.Add(new(rp.IndexVariable.Trim(), node.Id, title, "repeat index"));
        }
        return list;
    }
}
