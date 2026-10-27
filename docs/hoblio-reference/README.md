# Referência Hoblio (Rust)

Cópias dos documentos do repositório upstream [Hoblio](https://github.com/davidpestilli/hoblio) úteis para entender o **formato de fluxo**, produto e troubleshooting. O runtime deste repositório (**Elfio**) é C# / WPF; os guias abaixo descrevem em grande parte o comportamento compartilhado (JSON, ações, exemplos).

| Arquivo | Descrição |
|---------|-----------|
| [guia-utilizacao.md](guia-utilizacao.md) | Studio Rust, CLI, exemplos, ações |
| [guia-construcao.md](guia-construcao.md) | Arquitetura Rust, build, QA |
| [troubleshooting-windows.md](troubleshooting-windows.md) | MSVC, WebView2, browser, disco |
| [produto/decisoes-produto.md](produto/decisoes-produto.md) | Escopo MVP |
| [mvp-flow-control.md](mvp-flow-control.md) | Slice 5 — controle de fluxo |
| [mvp-browser-resilience.md](mvp-browser-resilience.md) | Slice 8 — browser e resiliência |

Para build e comandos **deste** repo, use o [README](../../README.md) na raiz (`.NET 8`, `dotnet test`, `Hoblio.Studio`).
