namespace Hoblio.Studio;

public sealed record PaletteEntry(string Key, string Title, string? Hint = null, string? SearchAliases = null);

public sealed class PaletteSection
{
    public required string Name { get; init; }
    public bool DefaultExpanded { get; init; }
    public IList<PaletteEntry> Items { get; init; } = [];
}

public sealed class PaletteGroup
{
    public required string Name { get; init; }
    public bool DefaultExpanded { get; init; }
    public IList<PaletteEntry> Items { get; init; } = [];
    public IList<PaletteSection> Sections { get; init; } = [];
}

/// <summary>Ações e controles agrupados por categoria de produto (Studio).</summary>
public static class ActionPaletteCatalog
{
    public static IReadOnlyList<PaletteGroup> Groups { get; } =
    [
        new PaletteGroup
        {
            Name = "Utilitários",
            Items =
            [
                new("node:start", "Início", "Entrada do fluxo (máx. um Start por grafo)",
                    "Start node start"),
                new("node:end", "Fim", "Saída do fluxo", "End node end"),
                new("action:log", "Log", "Escreve no log de execução", "LogAction log"),
                new("action:no_op", "No-op", "Não faz nada (placeholder)", "NoOp noop"),
                new("action:delay", "Aguardar", "Aguarda milissegundos", "Delay wait sleep"),
                new("action:http_get", "HTTP GET", "Requisição HTTP GET", "HttpGet http rede web"),
                new("action:http_request", "HTTP / REST", "Invocar serviço web (método, cabeçalhos, corpo)",
                    "HttpRequest POST PATCH PUT InvokeWebService REST API supabase"),
                new("action:llm_completion", "LLM (Ollama / OpenAI)", "Prompt + documento + JSON opcional via POST",
                    "Ollama LM Studio InvokeLLM chat completions local"),
            ],
        },
        new PaletteGroup
        {
            Name = "Caixa de mensagens",
            Items =
            [
                new("action:show_message", "Mostrar mensagem", "Diálogo informativo",
                    "ShowMessage messagebox dialog"),
                new("action:show_get_user_input", "Perguntar ao usuário", "Pergunta ao usuário",
                    "ShowGetUserInput input prompt"),
                new("action:show_select_from_list", "Escolher da lista", "Lista de opções em diálogo",
                    "ShowSelectFromList list selection combo"),
            ],
        },
        new PaletteGroup
        {
            Name = "Arquivos",
            Items =
            [
                new("action:file_write", "Gravar arquivo", "Cria ou sobrescreve arquivo",
                    "FileWrite write file"),
                new("action:file_append", "Anexar ao arquivo", "Acrescenta linhas", "FileAppend append"),
                new("action:file_read", "Ler arquivo", "Lê arquivo para variável", "FileRead read file"),
            ],
        },
        ExcelPaletteCatalog.Group,
        WordPaletteCatalog.Group,
        PdfPaletteCatalog.Group,
        BrowserPaletteCatalog.Group,
        VariablesPaletteCatalog.Group,
        new PaletteGroup
        {
            Name = "Fluxos",
            Items =
            [
                new("action:run_flow", "Executar fluxo", "Chama subfluxo ou flow.json filho",
                    "Run flow subflow"),
                new("ui:create_subflow", "Novo subfluxo", "Cria subfluxo embutido neste flow.json",
                    "New subflow create"),
            ],
        },
        new PaletteGroup
        {
            Name = "Condicionais",
            Items =
            [
                new("if", "Se (If)", "Ramifica then/else", "If conditional branch"),
                new("switch", "Switch", "Compara expressão em cadeia de cases", "Switch conditional"),
                new("case", "Case", "Ramo do switch (then/else)", "Case switch branch"),
                new("case_default", "Default case", "Ramo padrão do switch", "Default case switch"),
            ],
        },
        new PaletteGroup
        {
            Name = "Loops",
            Items =
            [
                new("for_each", "Para cada", "Itera lista", "For each foreach loop"),
                new("while_loop", "Enquanto", "Enquanto condição", "While loop"),
                new("repeat", "Repetir N vezes", "Repete N vezes", "Repeat N times"),
                new("next_loop", "Próximo loop", "Próxima iteração", "Next loop continue"),
                new("exit_loop", "Sair do loop", "Sai do loop", "Exit loop break"),
            ],
        },
    ];
}
