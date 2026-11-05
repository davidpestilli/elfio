namespace Hoblio.Model;

/// <summary>Automação de navegador via CDP (PuppeteerSharp). Sessão compartilhada por execução de fluxo.</summary>
public sealed class WebAutomationAction : ActionKind
{
    public string Operation { get; init; } = WebAutomationActionTypes.Navigate;

    public override string Type => Operation;

    public string? Url { get; init; }
    public ulong? WaitMs { get; init; }
    public bool? ShowOnScreen { get; init; }
    public bool? StartMaximized { get; init; }

    public string? Selector { get; init; }
    public ulong? TimeoutMs { get; init; }
    public bool? WaitForNavigation { get; init; }
    public string? WaitSelectorAfter { get; init; }
    public ulong? WaitAfterTimeoutMs { get; init; }

    public string? Text { get; init; }
    public string? PopulateMode { get; init; }
    public bool? WaitVisible { get; init; }

    public string? Variable { get; init; }
    public string? Detail { get; init; }

    public string? Script { get; init; }
    public string? Path { get; init; }

    public bool? Checked { get; init; }
    public string? DropdownMode { get; init; }
    public string? DropdownValue { get; init; }

    public string? WaitContentMode { get; init; }

    public string? BrowserEngine { get; init; }

    public string? AttachMode { get; init; }
    public ulong? DebuggingPort { get; init; }

    public override IEnumerable<string> ProducedVariables()
    {
        if (!string.IsNullOrWhiteSpace(Variable))
            yield return Variable.Trim();
    }
}
