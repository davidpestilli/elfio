# Guia de construção — Elfio (Hoblio C#)

Repositório: [github.com/davidpestilli/elfio](https://github.com/davidpestilli/elfio)

## Stack

| Componente | Tecnologia |
|------------|------------|
| Modelo / JSON | .NET 8, `System.Text.Json` (snake_case) |
| Runtime | PuppeteerSharp (CDP), HTTP, arquivos, diálogos WPF |
| CLI | `System.CommandLine` (`hoblio validate \| run`) |
| Studio | WPF + [Nodify](https://github.com/miroiu/nodify) 6.x |

Referência de produto e formato de fluxo (repo Rust): [hoblio-reference/README.md](hoblio-reference/README.md).

## Build e testes

```powershell
git clone https://github.com/davidpestilli/elfio.git
cd elfio
dotnet build
dotnet test
```

Paridade com exemplos locais:

```powershell
.\scripts\verify-parity.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\eval-grok-cross-stack.ps1
```

Roteiro Grok v2 (L1/L2/L3): [avaliacao/protocolos/eval-grok-roteiro-v2.md](avaliacao/protocolos/eval-grok-roteiro-v2.md).

## Avaliação Grok (27/10/2026, v2)

Relatório: `reports/eval-cross-stack-2026-10-27-v2/` (local, não versionado).

| Nível | Resultado antes dos fixes pós-Grok |
|-------|-------------------------------------|
| **L1** | APROVADO (harness 54 PASS) — ressalvas BOM/contagem bytes |
| **L2** | REPROVADO — probe SC03 `browser_wait_navigation` |
| **L3** | PARCIAL 8/11 — Stop, arestas canvas, destaque execução |

### Correções aplicadas após a rodada v2

| Item | Ação |
|------|------|
| **file_write BOM** | Gravação UTF-8 **sem BOM** (`UTF8Encoding(false)`) |
| **file_append / file_read** | Resumos usam **contagem de bytes** UTF-8 |
| **file_read (continue)** | Erro com contexto `read file <path>` (`HoblioRunException`) |
| **CLI E01-STRICT** | Erros via `Environment.ExitCode` — sem `Unhandled exception` após `Caused by:` |
| **Stdout UTF-8** | `Console.OutputEncoding` UTF-8 no CLI |
| **browser_wait_navigation** | Se a página já está `complete`/`interactive`, não espera navegação pendente (alinhado ao pós-`navigate`) |
| **Studio Stop** | `RaiseCanExecuteChanged` em Run/Stop; cancelamento cooperativo no `delay` |
| **Arestas Nodify** | `LineConnection` usa `SourcePoint`/`TargetPoint` derivados da posição dos nós |
| **Destaque execução** | Borda/fundo laranja quando `IsActive` |
| **Remover aresta** | Remove só a aresta `(from, to, port)` selecionada |
| **Palette** | Posicionamento em grade ao adicionar nós |

### Pendências conhecidas

- **S12 Waypoints** — paridade com `WaypointEdge` (React Flow); não implementado.
- **StrictSummaryLines** — contagem de linhas Rust ≠ C# (tracing duplicado no Rust); informativo apenas.
- **`failed (continued)`** — mensagem com cadeia `Caused by:`; alinhar texto 1:1 ao Rust se necessário.

## Projetos de fluxo

Os demos ficam em `examples/` (mesmo JSON do [Hoblio Rust](https://github.com/davidpestilli/hoblio)). Variável opcional `HOBLIO_EXAMPLES_ROOT` para apontar outra pasta.

## Variáveis de ambiente

| Variável | Uso |
|----------|-----|
| `HOBLIO_CHROME_PATH` | Executável Chromium |
| `HOBLIO_BROWSER_TEST=1` | Habilita teste de navegação local |
| `HOBLIO_DEBUG=1` | Stack trace completa na CLI |
| `HOBLIO_EXAMPLES_ROOT` | Pasta base dos exemplos nos testes |
