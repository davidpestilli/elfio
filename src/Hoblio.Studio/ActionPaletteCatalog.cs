namespace Hoblio.Studio;

public sealed record PaletteEntry(string Key, string Title, string? Hint = null);

public sealed class PaletteGroup
{
    public required string Name { get; init; }
    public IList<PaletteEntry> Items { get; init; } = [];
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
                new("node:start", "Start node", "Entrada do fluxo (máx. um Start por grafo)"),
                new("node:end", "End node", "Saída do fluxo"),
                new("action:log", "Log", "Escreve no log de execução"),
                new("action:no_op", "NoOp", "Não faz nada (placeholder)"),
                new("action:delay", "Delay", "Aguarda milissegundos"),
                new("action:http_get", "HttpGet", "Requisição HTTP GET"),
            ],
        },
        new PaletteGroup
        {
            Name = "Caixa de mensagens",
            Items =
            [
                new("action:show_message", "ShowMessage", "Diálogo informativo"),
                new("action:show_get_user_input", "ShowGetUserInput", "Pergunta ao usuário"),
            ],
        },
        new PaletteGroup
        {
            Name = "Arquivos",
            Items =
            [
                new("action:file_write", "FileWrite", "Cria ou sobrescreve arquivo"),
                new("action:file_append", "FileAppend", "Acrescenta linhas"),
                new("action:file_read", "FileRead", "Lê arquivo para variável"),
            ],
        },
        new PaletteGroup
        {
            Name = "Browser",
            Items =
            [
                new("action:browser_navigate", "BrowserNavigate", "Abre URL no Chrome/Edge"),
                new("action:browser_click", "BrowserClick", "Clique em seletor CSS"),
                new("action:browser_wait_navigation", "Wait navigation", "Espera navegação"),
                new("action:browser_fill", "BrowserFill", "Preenche campo"),
                new("action:browser_wait_for", "BrowserWaitFor", "Espera elemento"),
                new("action:browser_get_text", "BrowserGetText", "Captura texto"),
            ],
        },
        new PaletteGroup
        {
            Name = "Variáveis",
            Items =
            [
                new("action:set_variable", "Set variable", "Define valor"),
                new("action:increase_variable", "Increase variable", "Incrementa número"),
                new("action:decrease_variable", "Decrease variable", "Decrementa número"),
                new("action:list_clear", "List clear", "Esvazia lista"),
                new("action:list_append", "List append", "Adiciona item à lista"),
            ],
        },
        new PaletteGroup
        {
            Name = "Fluxos",
            Items =
            [
                new("action:run_flow", "Run flow", "Chama subfluxo ou flow.json filho"),
                new("ui:create_subflow", "New subflow", "Cria subfluxo embutido neste flow.json"),
            ],
        },
        new PaletteGroup
        {
            Name = "Condicionais",
            Items = [new("if", "If", "Ramifica then/else")],
        },
        new PaletteGroup
        {
            Name = "Loops",
            Items =
            [
                new("for_each", "For each", "Itera lista"),
                new("while_loop", "While", "Enquanto condição"),
                new("repeat", "Repeat N times", "Repete N vezes"),
                new("next_loop", "Next loop", "Próxima iteração"),
                new("exit_loop", "Exit loop", "Sai do loop"),
            ],
        },
    ];
}
