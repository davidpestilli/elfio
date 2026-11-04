# Arquitetura — Elfio (Hoblio C#)

## Correspondência com o repo Rust

| Rust | C# | Responsabilidade |
|------|-----|------------------|
| `hoblio-model` | `Hoblio.Model` | `FlowGraph`, `ActionKind`, `ControlKind`, `GraphValidator`, `TemplateReferenceValidator` |
| `hoblio-serde` | `Hoblio.Serde` | `ProjectLoader.LoadProject` / `SaveProject` |
| `hoblio-runtime` | `Hoblio.Runtime` | `FlowExecutor`, `ActionExecutor`, browser, arquivos, expressões |
| `hoblio-log` | (parcial) | CLI `--jsonl` → `.hoblio/run.jsonl` |
| `hoblio-cli` | `Hoblio.Cli` | `validate`, `run` |
| `hoblio-studio` | `Hoblio.Studio` | WPF + WebView2/React Flow (canvas, abrir/salvar/validar/executar) |

## Formato on-disk

Inalterado em relação ao Hoblio original:

- `manifest.json` — nome, versão, descrição
- `flow.json` — `start`, `nodes`, `edges`, `variables` opcional

JSON usa `snake_case` e discriminated unions (`type` em ações e controles), espelhando `serde` no Rust.

## Execução

1. `GraphValidator.ValidateProject`
2. Interpretador de grafo em `FlowExecutor` (If, ForEach, While, Repeat, Next/Exit loop)
3. Ações via `ActionExecutor` + política `ErrorHandling` (retry / continue)
4. Browser: sessão única por run (`BrowserContext` + PuppeteerSharp)

Detalhe de camadas, catálogo `module`/`action_id`, JSONL e extensão do manifest: [construcao/README.md](construcao/README.md).

## Studio

Shell **WPF** (ribbon, paleta, propriedades, log) + **WebView2** hospedando o bundle Vite `flow-canvas/` (React Flow). Estado do grafo e histórico ficam no `FlowEditorViewModel`; o canvas troca mensagens JSON via `FlowCanvasHost` (`FlowEditorViewModel.FlowCanvas.cs`): `setGraph`, seleção, drag, waypoints, reconectar, toolbar (modo/undo/limpar/rótulo), clipboard.

Palette agrupada, subfluxos/`manifest.flows`, painel por tipo de ação, índice **Flow variables**, variáveis de projeto, `run_flow`. Build do canvas: `scripts/build-flow-canvas.ps1` ou MSBuild quando `dist/` falta. Checklist em [PARITY.md](PARITY.md); uso em [studio-guia.md](studio-guia.md).

**Validação:** `GraphValidator.ValidateProject` inclui checagem de `{refs}` em templates contra variáveis de projeto e produtores do fluxo. **Fora de escopo (Rust também):** ramo **On error** dedicado no grafo além de retry/continue no nó.

**Extensão C#:** `http_get` aceita campo JSON opcional `variable` para gravar o corpo da resposta (Studio expõe o campo no painel).
