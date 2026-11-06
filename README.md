# Elfio

**Elfio** é uma plataforma de automação de fluxos (RPA) para **Windows**, escrita em **.NET 8**. Você descreve processos como um **grafo visual** de ações; o runtime executa navegador, arquivos, variáveis, controle de fluxo e integrações com **Microsoft Excel** e **Word** via COM.

Repositório: [github.com/davidpestilli/elfio](https://github.com/davidpestilli/elfio)

## O que você encontra aqui

| Peça | Descrição |
|------|-----------|
| **Elfio Studio** | Editor desktop (WPF + WebView2) com canvas [React Flow](https://reactflow.dev/) — paleta de ações, propriedades, validação e execução com log |
| **CLI** | `validate` e `run` sobre pastas de projeto (`manifest.json` + `flow.json`) |
| **Runtime** | Motor de execução: PuppeteerSharp/CDP para web, I/O de arquivos, listas, diálogos nativos, Excel/Word |
| **Modelo & serialização** | Grafo tipado, catálogo de ações, carga/gravação JSON (`snake_case`) |

Projetos de automação são **pastas versionáveis**: metadados em `manifest.json`, grafo principal em `flow.json`, subfluxos opcionais em `flows/`. Veja [docs/construcao/manifest-e-capacidades.md](docs/construcao/manifest-e-capacidades.md).

## Capacidades (visão geral)

- **Controle de fluxo** — condicionais, loops, subfluxos, composição de fluxos
- **Superfluxos** — um fluxo principal pode chamar o fluxo principal de **outro projeto** (`run_flow.project` + `manifest.linked_projects`) herdando sessão de navegador e variáveis; veja [docs/construcao/composicao-superfluxos.md](docs/construcao/composicao-superfluxos.md)
- **Variáveis** — tabelas, listas, CSV, transformações e referências `{nome}` entre nós
- **Web** — navegação e automação via Chrome/Edge (CDP); anexar a instância já aberta
- **Arquivos & UI** — leitura/escrita, seleção em lista, entrada de usuário (diálogos WPF)
- **Excel & Word** — ações COM alinhadas a vocabulário de produto RPA (demos em `examples/`)

Exemplos prontos: `flow-control-demo`, `browser-demo-flow`, `excel-demo-flow`, `word-demo-flow`, `variables-demo-flow`, `login-eproc`, `superflow-demo` (encadeia `superflow-login` → `superflow-petition`), entre outros em [`examples/`](examples/).

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows** (runtime browser, COM Office, diálogos nativos)
- **Node.js 20+** — build do canvas Vite na primeira compilação do Studio
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
- **Chrome ou Edge** para ações `browser_*` (ou variável `HOBLIO_CHROME_PATH` se a detecção automática falhar)
- **Microsoft Excel** — demos `excel_*` (ex.: `examples/excel-demo-flow`)
- **Microsoft Word** — demos `word_*` (ex.: `examples/word-demo-flow`)

Clone o repositório em uma pasta cujo caminho **não contenha `#`** (o bundler Vite falha com esse caractere). Recomendado: diretório `elfio`. Detalhes: [docs/rename-to-elfio.md](docs/rename-to-elfio.md).

## Início rápido

```powershell
git clone https://github.com/davidpestilli/elfio.git
cd elfio
.\scripts\build-flow-canvas.ps1   # opcional; dotnet build também gera dist/ se faltar
dotnet build
dotnet test
```

Validar e executar um demo pela CLI:

```powershell
dotnet run --project src/Hoblio.Cli -- validate examples/flow-control-demo
dotnet run --project src/Hoblio.Cli -- run examples/flow-control-demo
dotnet run --project src/Hoblio.Cli -- run examples/excel-demo-flow --jsonl
dotnet run --project src/Hoblio.Cli -- run examples/word-demo-flow --jsonl
```

Abrir o **Elfio Studio**:

```powershell
dotnet run --project src/Hoblio.Studio
```

Abrir um navegador **com depuração (CDP)** para um fluxo se anexar (`browser_attach_chrome` / `browser_attach_edge`):

```powershell
dotnet run --project src/Hoblio.Cli -- browser --port 9222 --url https://eproc-1g-sp-hml.tjsp.jus.br/
```

No Studio é o botão **Navegador → Abrir CDP…** do ribbon (mesmo serviço, pré-preenchido a partir do nó de anexar). O navegador usa um perfil dedicado, `%TEMP%\elfio-cdp-profile`, e fica aberto depois do run — é nele que você faz o login.

Atualizar capacidades no `manifest` após mudar ações no grafo (sem abrir o Studio):

```powershell
dotnet run --project src/Hoblio.Cli -- validate examples/browser-demo-flow --update-manifest
```

Gerar ícones do aplicativo a partir de `Assets/elf_domestic.webp`:

```powershell
.\scripts\generate-elfio-icons.ps1
```

## Estrutura do repositório

```
src/
  Hoblio.Model/      # Grafo, ações, validação
  Hoblio.Serde/      # Load/save de projeto
  Hoblio.Runtime/    # Executor e implementações de ações
  Hoblio.Cli/         # Ferramenta de linha de comando
  Hoblio.Studio/      # Shell WPF + flow-canvas/ (React + Vite)
examples/             # Projetos de demonstração
tests/                # Testes unitários e de integração
docs/                 # Arquitetura, módulos, guia do Studio
scripts/              # Build do canvas, ícones, verificação local
```

## Documentação

| Recurso | Conteúdo |
|---------|----------|
| [docs/README.md](docs/README.md) | Índice completo |
| [docs/studio-guia.md](docs/studio-guia.md) | Elfio Studio — layout e fluxo de trabalho |
| [docs/guia-construcao.md](docs/guia-construcao.md) | Build, testes, histórico de entregas |
| [docs/construcao/README.md](docs/construcao/README.md) | Runtime, catálogo de ações, telemetria, manifest |

## Desenvolvimento

```powershell
dotnet build -c Release
dotnet test
.\scripts\verify-parity.ps1   # validação Release + demos listados em docs/PARITY.md
```

Variáveis úteis para browser (anexar ou override de executável): `HOBLIO_CHROME_PATH`, `HOBLIO_BROWSER_DEBUG_PORT`, `HOBLIO_BROWSER_DEBUG_URL`. Exemplos alternativos de fluxos: `HOBLIO_EXAMPLES_ROOT`.
