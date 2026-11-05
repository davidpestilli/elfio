namespace Hoblio.Model;

/// <summary>Executable action attached to a flow node.</summary>
public abstract class ActionKind
{
    public abstract string Type { get; }

    public abstract IEnumerable<string> ProducedVariables();
}

public sealed class LogAction : ActionKind
{
    public override string Type => "log";
    public required string Message { get; init; }

    public override IEnumerable<string> ProducedVariables() => [];
}

public sealed class NoOpAction : ActionKind
{
    public override string Type => "no_op";
    public override IEnumerable<string> ProducedVariables() => [];
}

public sealed class FileWriteAction : ActionKind
{
    public override string Type => "file_write";
    public required string Path { get; init; }
    public required string Content { get; init; }
    public override IEnumerable<string> ProducedVariables() => [];
}

public sealed class FileAppendAction : ActionKind
{
    public override string Type => "file_append";
    public required string Path { get; init; }
    public required string Content { get; init; }
    public override IEnumerable<string> ProducedVariables() => [];
}

public sealed class FileReadAction : ActionKind
{
    public override string Type => "file_read";
    public required string Path { get; init; }
    public required string Variable { get; init; }
    public override IEnumerable<string> ProducedVariables()
    {
        if (!string.IsNullOrWhiteSpace(Variable))
            yield return Variable;
    }
}

public sealed class ListAppendAction : ActionKind
{
    public override string Type => "list_append";
    public required string ListVariable { get; init; }
    public required string Item { get; init; }
    public override IEnumerable<string> ProducedVariables() => [];
}

public sealed class ListClearAction : ActionKind
{
    public override string Type => "list_clear";
    public required string ListVariable { get; init; }
    public override IEnumerable<string> ProducedVariables() => [];
}

public sealed class DelayAction : ActionKind
{
    public override string Type => "delay";
    public required ulong Milliseconds { get; init; }
    public override IEnumerable<string> ProducedVariables() => [];
}

public sealed class HttpGetAction : ActionKind
{
    public override string Type => "http_get";
    public required string Url { get; init; }
    public string? Variable { get; init; }

    public override IEnumerable<string> ProducedVariables()
    {
        if (!string.IsNullOrWhiteSpace(Variable))
            yield return Variable.Trim();
    }
}

public sealed class ShowMessageAction : ActionKind
{
    public override string Type => "show_message";
    public required string Title { get; init; }
    public required string Text { get; init; }
    public double? TimeoutSeconds { get; init; }
    public override IEnumerable<string> ProducedVariables() => [];
}

public sealed class ShowGetUserInputAction : ActionKind
{
    public override string Type => "show_get_user_input";
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string DefaultValue { get; init; } = "";
    public UserInputKind InputKind { get; init; } = UserInputKind.SingleLine;
    public bool? Topmost { get; init; }
    public string UserInputVariable { get; init; } = "UserInput";
    public string ButtonPressedVariable { get; init; } = "ButtonPressed";
    public override IEnumerable<string> ProducedVariables()
    {
        if (!string.IsNullOrEmpty(UserInputVariable))
            yield return UserInputVariable;
        if (!string.IsNullOrEmpty(ButtonPressedVariable))
            yield return ButtonPressedVariable;
    }
}

public sealed class ShowSelectFromListAction : ActionKind
{
    public override string Type => "show_select_from_list";
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string List { get; init; }
    public bool? Topmost { get; init; }
    public bool? LimitToList { get; init; } = true;
    public bool? AllowEmptySelection { get; init; }
    public bool? AllowMultipleSelections { get; init; }
    public string SelectedItemVariable { get; init; } = "SelectedItem";
    public string SelectedIndexVariable { get; init; } = "SelectedIndex";
    public string ButtonPressedVariable { get; init; } = "ButtonPressed";
    public override IEnumerable<string> ProducedVariables()
    {
        if (!string.IsNullOrEmpty(SelectedItemVariable))
            yield return SelectedItemVariable;
        if (!string.IsNullOrEmpty(SelectedIndexVariable))
            yield return SelectedIndexVariable;
        if (!string.IsNullOrEmpty(ButtonPressedVariable))
            yield return ButtonPressedVariable;
    }
}

public sealed class SetVariableAction : ActionKind
{
    public override string Type => "set_variable";
    public required string Variable { get; init; }
    public required string Value { get; init; }
    public override IEnumerable<string> ProducedVariables()
    {
        if (!string.IsNullOrEmpty(Variable))
            yield return Variable;
    }
}

public sealed class IncreaseVariableAction : ActionKind
{
    public override string Type => "increase_variable";
    public required string Variable { get; init; }
    public string? Amount { get; init; }
    public override IEnumerable<string> ProducedVariables() => [];
}

public sealed class DecreaseVariableAction : ActionKind
{
    public override string Type => "decrease_variable";
    public required string Variable { get; init; }
    public string? Amount { get; init; }
    public override IEnumerable<string> ProducedVariables() => [];
}

public sealed class RunFlowAction : ActionKind
{
    public override string Type => "run_flow";

    /// <summary>
    /// Projeto vinculado (<c>manifest.linked_projects</c>) onde o fluxo será resolvido.
    /// Quando ausente, o fluxo é resolvido no próprio projeto.
    /// </summary>
    public string? Project { get; init; }

    public string? FlowId { get; init; }
    public string? Path { get; init; }
    public Dictionary<string, string>? Inputs { get; init; }
    public List<string>? Outputs { get; init; }
    public bool? ShareBrowser { get; init; }
    public RunFlowVariableScope? VariableScope { get; init; }

    public override IEnumerable<string> ProducedVariables()
    {
        if (Outputs is null)
            yield break;
        foreach (var name in Outputs)
        {
            if (!string.IsNullOrWhiteSpace(name))
                yield return name;
        }
    }
}
