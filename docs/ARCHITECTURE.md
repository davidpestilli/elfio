# Arquitetura — Hoblio C#

## Correspondência com o repo Rust

| Rust | C# | Responsabilidade |
|------|-----|------------------|
| `hoblio-model` | `Hoblio.Model` | `FlowGraph`, `ActionKind`, `ControlKind`, `GraphValidator` |
| `hoblio-serde` | `Hoblio.Serde` | `ProjectLoader.LoadProject` / `SaveProject` |
| `hoblio-runtime` | `Hoblio.Runtime` | `FlowExecutor`, `ActionExecutor`, browser, arquivos, expressões |
| `hoblio-log` | (parcial) | CLI `--jsonl` → `.hoblio/run.jsonl` |
| `hoblio-cli` | `Hoblio.Cli` | `validate`, `run` |
| `hoblio-studio` | `Hoblio.Studio` | WPF + Nodify (canvas, abrir/salvar/validar/executar) |

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

## Studio

Fase inicial do port: **visualização e edição de layout** (posição `ui`), toolbar abrir/salvar/validar/executar. Painel de propriedades rico (como `PropertiesPanel.tsx`) fica listado em [PARITY.md](PARITY.md) como pendência de UX.
