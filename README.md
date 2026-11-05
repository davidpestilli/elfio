# Elfio — Hoblio em C# / WPF / React Flow

Repositório [**elfio**](https://github.com/davidpestilli/elfio): porta do [Hoblio](https://github.com/davidpestilli/hoblio) (Rust + Tauri + React Flow) para **.NET 8**, mantendo o mesmo formato `manifest.json` + `flow.json`.

Clone recomendado em pasta **`elfio`** (sem `#` no caminho) para build do canvas Vite. Se ainda estiver em `hoblio-c#`, veja [docs/rename-to-elfio.md](docs/rename-to-elfio.md).

- **Hoblio.Model** — grafo, ações, validação (`hoblio-model`)
- **Hoblio.Serde** — load/save de projeto
- **Hoblio.Runtime** — executor, browser (PuppeteerSharp / CDP), arquivos, listas, diálogos Win32/WPF
- **Hoblio.Cli** — `hoblio validate | run`
- **Hoblio.Studio** — shell WPF + WebView2 + React Flow (`src/Hoblio.Studio/flow-canvas/`)

## Pré-requisitos

- .NET 8 SDK
- Windows (runtime browser + diálogos nativos)
- Node.js 20+ (build do `flow-canvas` na primeira compilação do Studio)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
- Chrome ou Edge (ações `browser_*`), ou `HOBLIO_CHROME_PATH`
- Microsoft Excel instalado (ações `excel_*`, ex.: `examples/excel-demo-flow`)

## Comandos

```powershell
git clone https://github.com/davidpestilli/elfio.git
cd elfio
.\scripts\build-flow-canvas.ps1   # opcional; dotnet build também dispara se dist/ faltar
dotnet build
dotnet test
dotnet run --project src/Hoblio.Cli -- validate examples/flow-control-demo
dotnet run --project src/Hoblio.Cli -- run examples/flow-control-demo
dotnet run --project src/Hoblio.Cli -- run examples/excel-demo-flow --jsonl
dotnet run --project src/Hoblio.Studio
```

Ícone do app (a partir de `Assets/elf_domestic.webp`):

```powershell
.\scripts\generate-elfio-icons.ps1
```

Paridade local e harness comparativo com o repo Rust (clone irmão `hoblio` ou variável `HOBLIO_EXAMPLES_ROOT`):

```powershell
.\scripts\verify-parity.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\eval-grok-cross-stack.ps1
```

Ver [docs/avaliacao/protocolos/eval-grok-roteiro-v2.md](docs/avaliacao/protocolos/eval-grok-roteiro-v2.md) para avaliação Grok completa.

## Documentação

- [docs/README.md](docs/README.md) — índice
