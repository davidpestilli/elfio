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

Editor WPF + Nodify: canvas, palette agrupada, subfluxos/`manifest.flows`, painel de propriedades por tipo de ação, variáveis de projeto, `run_flow`, clipboard (Ctrl+C/X/V), waypoints e ligações no painel. Checklist detalhado em [PARITY.md](PARITY.md); uso em [studio-guia.md](studio-guia.md).

**Fora de escopo (Rust também):** validação estática de `{refs}` desconhecidas; ramo **On error** dedicado no grafo além de retry/continue no nó.
