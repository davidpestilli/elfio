# Guia de construção — Elfio (Hoblio C#)

Repositório: [github.com/davidpestilli/elfio](https://github.com/davidpestilli/elfio)

## Stack

| Componente | Tecnologia |
|------------|------------|
| Modelo / JSON | .NET 8, `System.Text.Json` (snake_case) |
| Runtime | PuppeteerSharp (CDP), HTTP, arquivos, diálogos WPF |
| CLI | `System.CommandLine` (`hoblio validate \| run`) |
| Studio | WPF + WebView2 + [React Flow](https://reactflow.dev/) (`flow-canvas/`) |

Referência de produto e formato de fluxo (repo Rust): [hoblio-reference/README.md](hoblio-reference/README.md).

Arquitetura de execução, identificadores de ação, logs e manifest: [construcao/README.md](construcao/README.md).

Atualizar capacidades no manifest sem abrir o Studio:

```powershell
dotnet run --project src/Hoblio.Cli -- validate examples/browser-demo-flow --update-manifest
```

## Build e testes

```powershell
git clone https://github.com/davidpestilli/elfio.git
cd elfio
.\scripts\build-flow-canvas.ps1   # se dist/ não existir; ver nota abaixo sobre '#' no path
dotnet build
dotnet test
```

**Canvas:** o Studio embute `src/Hoblio.Studio/flow-canvas/dist/` (gerado com Node 20+). O `dotnet build` do Studio dispara o build do Vite quando `dist/` falta. Caminhos com `#` (ex. pasta antiga `hoblio-c#`) quebram o Vite — use pasta `elfio` ou [rename-to-elfio.md](rename-to-elfio.md) / `build-flow-canvas.ps1` (build em `%TEMP%`).

**Ícone:** `.\scripts\generate-elfio-icons.ps1` (fonte `Assets/elf_domestic.webp` → PNG/ICO do exe e ribbon).

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
| **Destaque execução** | Destaque no nó ativo no canvas React Flow (`isActive`) |
| **Remover aresta** | Delete com ligação selecionada no canvas |
| **Palette** | Posicionamento em grade ao adicionar nós (`PaletteSpawn`) |

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

## Rodada nov/2026 — React Flow, `login-eproc` e polish do Studio

Resumo do que entrou nesta rodada (paridade funcional com o Hoblio Rust/Tauri para edição e execução do exemplo `examples/login-eproc`).

### Studio — canvas e shell

| Área | Mudança |
|------|---------|
| **Diagrama** | Nodify / `WaypointOverlay` / `FlowConnectionView` removidos; canvas único em **WebView2** (`FlowCanvas/`, app Vite em `flow-canvas/`). |
| **Bridge** | Mensagens JSON canvas ↔ `FlowEditorViewModel.FlowCanvas.cs` (grafo, seleção, undo/redo, clipboard, toolbar, waypoints, reconectar arestas). |
| **Toolbar do canvas** | Modo **Exibição / Edição**, desfazer/refazer, apagar seleção, limpar fluxo, barra de **rótulo de aresta** — espelha o Rust; barra WPF duplicada foi retirada. |
| **React Flow** | `WaypointEdge`, handles `then/else/body/after`, `edgesReconnectable`, substituir ligação na mesma porta, snap 10×10, guias de alinhamento, `fitViewToken`, MiniMap/Controls, atalhos (copiar/colar/recortar/selecionar tudo). |
| **Propriedades** | Índice **Flow variables** (produtores + jump para nó); removido bloco **Nova ligação** — ligações só no canvas (como no Rust). |
| **Ribbon / janela** | Faixa WPF: Arquivo + Validar/Executar/Parar; abas de fluxo/subfluxo; status + **`DocumentTitle`** (nome do manifest + `*` se sujo) + caminho do projeto. Moldura OS: título fixo **Elfio Studio**, sem ícone na title bar (marca e ícone só no ribbon; taskbar usa `ApplicationIcon` do exe). |
| **Marca** | Scripts `generate-elfio-icons.ps1` / `.py`, assets `Hoblio.Studio/Assets/`, favicon do canvas. |

### Runtime e modelo (impacto em RPA / login eproc)

| Área | Mudança |
|------|---------|
| **`browser_navigate`** | Interpolação de `{variáveis}` na URL antes de navegar. |
| **`BrowserContext`** | Estabilidade pós-navegação, `WaitForSelector`, leituras via element handle, retries quando o contexto CDP é destruído. |
| **`show_message`** | Diálogo **bloqueante** em thread STA (`RunShowMessage`) — browser permanece até OK (comportamento do Rust). |
| **`GraphValidator`** | Várias ações podem escrever na mesma variável de run; removida regra de produtor único. |
| **Projeto novo** | `DefaultFlow.Create()` — `start: ""`, sem nós (Start/End pela paleta **Utilitários**). |
| **Exemplo** | `examples/login-eproc/flow.json` sincronizado com o repo Rust (`pick_environment`, HML/PRD, probes com `continue`). |

### Verificação

```powershell
.\scripts\verify-parity.ps1
dotnet run --project src/Hoblio.Cli -- validate examples/login-eproc
dotnet run --project src/Hoblio.Studio
```

Diferenças **esperadas** vs Rust (não bloqueiam paridade de JSON): motor CDP **PuppeteerSharp** vs driver Rust (timing/flakiness), **`TemplateReferenceValidator`** só no C#, texto do log CLI (`StrictSummaryLines` no harness). Detalhe em [PARITY.md](PARITY.md).

## Projetos de fluxo

Os demos ficam em `examples/` (mesmo JSON do [Hoblio Rust](https://github.com/davidpestilli/hoblio)). Variável opcional `HOBLIO_EXAMPLES_ROOT` para apontar outra pasta.

Padrões do login eproc (subfluxo de ambiente, `{EprocUrl}`, probes com `continue`): §4.5.1 em [hoblio-reference/guia-construcao.md](hoblio-reference/guia-construcao.md). Itens upstream espelhados nesta rodada estão na tabela **Rodada nov/2026** acima.

## Variáveis de ambiente

| Variável | Uso |
|----------|-----|
| `HOBLIO_CHROME_PATH` | Executável Chromium |
| `HOBLIO_BROWSER_TEST=1` | Habilita teste de navegação local |
| `HOBLIO_DEBUG=1` | Stack trace completa na CLI |
| `HOBLIO_EXAMPLES_ROOT` | Pasta base dos exemplos nos testes |
