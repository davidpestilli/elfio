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

/// <summary>
/// REST/HTTP request (PAD "Invocar serviço Web"): method, headers, optional body, response variables.
/// </summary>
public sealed class HttpRequestAction : ActionKind
{
    public override string Type => "http_request";

    public required string Url { get; init; }

    /// <summary>HTTP method: get, post, put, patch, delete, head, options (case-insensitive).</summary>
    public string Method { get; init; } = "get";

    /// <summary>Optional Accept header value (e.g. application/json).</summary>
    public string? Accept { get; init; }

    /// <summary>
    /// Optional request body; templates <c>{var}</c> are interpolated. Omitted or empty for GET/HEAD if not needed.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>Content-Type for the body (e.g. application/json). Used when <see cref="Body"/> is non-empty.</summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Extra headers, one per line: <c>Name: Value</c>. Lines support <c>{var}</c> interpolation.
    /// </summary>
    public string? CustomHeaders { get; init; }

    /// <summary>Variable to store the response body (optional).</summary>
    public string? ResponseBodyVariable { get; init; }

    /// <summary>Variable to store response headers as text (optional).</summary>
    public string? ResponseHeadersVariable { get; init; }

    /// <summary>Variable to store HTTP status code as string (optional).</summary>
    public string? StatusCodeVariable { get; init; }

    public override IEnumerable<string> ProducedVariables()
    {
        foreach (var v in new[] { ResponseBodyVariable, ResponseHeadersVariable, StatusCodeVariable })
        {
            if (!string.IsNullOrWhiteSpace(v))
                yield return v.Trim();
        }
    }
}

/// <summary>
/// Chama um LLM local ou compatível (Ollama <c>/api/chat</c>, OpenAI-style <c>/v1/chat/completions</c>): prompt, texto opcional do documento e esquema JSON opcional.
/// </summary>
public sealed class LlmCompletionAction : ActionKind
{
    public override string Type => "llm_completion";

    /// <summary>URL completa (ex.: <c>http://127.0.0.1:11434/api/chat</c> ou <c>https://api.openai.com/v1/chat/completions</c>).</summary>
    public required string Url { get; init; }

    /// <summary><c>ollama_chat</c> (predefinição) ou <c>openai_chat</c>.</summary>
    public string ApiStyle { get; init; } = "ollama_chat";

    public required string Model { get; init; }

    /// <summary>Mensagem de sistema (opcional).</summary>
    public string? System { get; init; }

    /// <summary>Mensagem do utilizador / instruções principais.</summary>
    public string? User { get; init; }

    /// <summary>Texto do documento ou contexto longo (opcional; interpolado).</summary>
    public string? Document { get; init; }

    /// <summary>
    /// Esquema ou restrição de formato JSON (opcional). Para Ollama é enviado como <c>format</c>;
    /// para OpenAI, se não estiver vazio, usa-se <c>response_format</c> com <c>type: json_object</c> quando o valor for o literal <c>json_object</c>, caso contrário tenta-se interpretar como JSON.
    /// </summary>
    public string? JsonSchema { get; init; }

    /// <summary>Temperatura (opcional, texto numérico).</summary>
    public string? Temperature { get; init; }

    /// <summary>Armazena o corpo JSON da resposta (obrigatório para produzir variável).</summary>
    public required string ResponseBodyVariable { get; init; }

    public override IEnumerable<string> ProducedVariables()
    {
        if (!string.IsNullOrWhiteSpace(ResponseBodyVariable))
            yield return ResponseBodyVariable.Trim();
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
