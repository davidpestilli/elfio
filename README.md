# Elfio — Hoblio em C# / WPF / Nodify

Repositório [**elfio**](https://github.com/davidpestilli/elfio): porta do [Hoblio](https://github.com/davidpestilli/hoblio) (Rust + Tauri + React Flow) para **.NET 8**, mantendo o mesmo formato `manifest.json` + `flow.json`.

- **Hoblio.Model** — grafo, ações, validação (`hoblio-model`)
- **Hoblio.Serde** — load/save de projeto
- **Hoblio.Runtime** — executor, browser (PuppeteerSharp / CDP), arquivos, listas, diálogos Win32/WPF
- **Hoblio.Cli** — `hoblio validate | run`
- **Hoblio.Studio** — editor flowchart com [Nodify](https://github.com/miroiu/nodify)

## Pré-requisitos

- .NET 8 SDK
- Windows (runtime browser + diálogos nativos)
- Chrome ou Edge (ações `browser_*`), ou `HOBLIO_CHROME_PATH`

## Comandos

```powershell
git clone https://github.com/davidpestilli/elfio.git
cd elfio
dotnet build
dotnet test
dotnet run --project src/Hoblio.Cli -- validate examples/flow-control-demo
dotnet run --project src/Hoblio.Cli -- run examples/flow-control-demo
dotnet run --project src/Hoblio.Studio
```

Paridade local e harness comparativo com o repo Rust (clone irmão `hoblio` ou variável `HOBLIO_EXAMPLES_ROOT`):

```powershell
.\scripts\verify-parity.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\eval-grok-cross-stack.ps1
```

Ver [docs/avaliacao/protocolos/eval-grok-roteiro-v2.md](docs/avaliacao/protocolos/eval-grok-roteiro-v2.md) para avaliação Grok completa.

## Documentação

- [docs/README.md](docs/README.md) — índice
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — mapa Rust → C#
- [docs/PARITY.md](docs/PARITY.md) — checklist de paridade
- [docs/hoblio-reference/](docs/hoblio-reference/) — guias de produto/utilização (origem Hoblio Rust)

## Layout

```
elfio/
  Hoblio.sln
  examples/          # demos de fluxo (mesmo JSON do Hoblio)
  src/Hoblio.*
  tests/
  scripts/
  docs/
```
