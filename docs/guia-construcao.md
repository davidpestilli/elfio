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

Paridade com exemplos (`examples/` neste repo, ou `HOBLIO_EXAMPLES_ROOT`, ou clone irmão `E:\Projetos\hoblio\examples`):

```powershell
.\scripts\verify-parity.ps1
```

O script faz `dotnet build -c Release`, `dotnet test`, `hoblio validate` nos demos listados em [PARITY.md](PARITY.md) e o teste filtrado `Browser_navigate_local_html` com `HOBLIO_BROWSER_TEST=1`.

Harness comparativo Rust ↔ C# (opcional, requer repo Rust):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\eval-grok-cross-stack.ps1
```

Espelhos C# dos roteiros Grok (flow-control e entregas A/B/C):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\eval-flow-control.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\eval-rpa-abc.ps1
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

| Área | Item |
|------|------|
| **Studio** | Ramo **On error** dedicado no grafo (fora de escopo no Rust também) — ver [PARITY.md](PARITY.md) |
| **CLI** | **StrictSummaryLines** — contagem de linhas Rust ≠ C# (tracing duplicado no Rust); informativo |

### Fechado recentemente

| Área | Item |
|------|------|
| **Modelo / CLI** | `validate` rejeita `{refs}` desconhecidas (`TemplateReferenceValidator`) |
| **Runtime / Studio** | `http_get` com campo opcional `variable` (corpo da resposta) |
| **CLI** | `failed (continued)` usa resumo em uma linha (`Caused by:` com `\|` no log de ação) |
| **Harness** | `scripts/eval-flow-control.ps1` e `scripts/eval-rpa-abc.ps1` (C#, IDs `GROK_EVAL` alinhados ao Rust) |
| **Studio** | Waypoints alinhados ao `WaypointEdge` (React Flow): viewport, + por segmento lógico, halo de seleção |

## Projetos de fluxo

Os demos ficam em `examples/` (mesmo JSON do [Hoblio Rust](https://github.com/davidpestilli/hoblio)). Variável opcional `HOBLIO_EXAMPLES_ROOT` para apontar outra pasta.

## Variáveis de ambiente

| Variável | Uso |
|----------|-----|
| `HOBLIO_CHROME_PATH` | Executável Chromium |
| `HOBLIO_BROWSER_TEST=1` | Habilita teste de navegação local |
| `HOBLIO_DEBUG=1` | Stack trace completa na CLI |
| `HOBLIO_EXAMPLES_ROOT` | Pasta base dos exemplos nos testes |
