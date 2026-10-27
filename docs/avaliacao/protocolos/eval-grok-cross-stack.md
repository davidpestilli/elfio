# Esquema de avaliação — paridade Rust vs C# (Grok bot)

Compara **Hoblio (Rust/Tauri)** e **Hoblio C# (WPF/Nodify)** nos mesmos `examples/*`, com saída no formato `GROK_EVAL` usado pelos harnesses existentes.

## Repositórios

| Stack | Caminho padrão |
|-------|----------------|
| Rust | `E:\Projetos\hoblio` |
| C# | `E:\Projetos\hoblio-c#` |
| Exemplos | `E:\Projetos\hoblio\examples` (ou junção `hoblio-c#\examples`) |

## Execução (obrigatória)

A partir de **qualquer** repo (o script infere o outro):

```powershell
# Opção A — raiz C#
cd E:\Projetos\hoblio-c#
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\eval-grok-cross-stack.ps1

# Opção B — raiz Rust
cd E:\Projetos\hoblio
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\eval-grok-cross-stack.ps1
```

Parâmetros opcionais:

```powershell
-File .\scripts\eval-grok-cross-stack.ps1 `
  -RustRoot E:\Projetos\hoblio `
  -CSharpRoot E:\Projetos\hoblio-c# `
  -ExamplesRoot E:\Projetos\hoblio\examples `
  -SkipBuild `
  -StrictSummaryLines
```

`-StrictSummaryLines` — opcional; exige também igualdade linha-a-linha das ações (ordem + texto). Use só quando depurar divergências de formatação.

## Formato de saída

```text
GROK_EVAL: <TEST_ID> PASS|<FAIL> [<detalhe>]
GROK_EVAL: META rust_root=... csharp_root=... examples=...
GROK_EVAL: SUMMARY pass=<n> fail=<n>
```

**Sucesso global:** exit code **0** e `fail=0`.

**Bloqueio:** exit code **2** se pré-requisitos (P1–P6) ou CLI C# Release ausente.

## Critérios de aprovação (Grok)

1. Todos os `P*` = PASS (rustc, cargo, dotnet, pastas).
2. `T-B01` / `T-B02` — build Release C# e `hoblio-cli` Rust (omitidos com `-SkipBuild` se binários já existirem).
3. `T-C01` — `dotnet test` no workspace C# (Release).  
   `T-C01-MIN` — total aprovado ≥ **27** (model + runtime).  
   Roteiro completo pós-correções: [`eval-grok-roteiro-v2.md`](eval-grok-roteiro-v2.md).
4. Para cada pasta `examples/*` com `manifest.json`:
   - `T-CMP-V-<nome>-R` — `cargo run -p hoblio-cli -- validate`
   - `T-CMP-V-<nome>-C` — `hoblio.exe validate`
   - `T-CMP-V-<nome>-EQ` — ambos exit 0
5. **Execução comparada** (exit 0, mesmo `last_node`, mesmos **marcadores** de texto na saída combinada stdout+stderr):
   - `T-CMP-E01` — `flow-control-demo`
   - `T-CMP-E02` — `variables-loops-demo`
   - `T-CMP-E03` — `demo-flow`
   - `T-CMP-E04` — `resilience-demo`
   - `T-CMP-E05` / `T-CMP-E05-REPORT` — `files-lists-demo` + conteúdo idêntico de `output/report.txt`
   - `T-CMP-E06` — cópia temp com `flag=no` (só ramo else)
6. **Negativo:** `T-CMP-N01-*` — grafo com duas arestas `main`; validate deve falhar nos dois stacks com `duplicate outgoing port`.
7. **Browser / CLI (v2):** `T-CLI-J01`, `T-CLI-V01`, `T-CLI-E01`, `T-CLI-E01-STRICT` (informativo); `T-BRW-UNIT`; `T-CMP-E07` — execução real `browser-demo-flow` + teste Puppeteer local.  
   **Importante:** rodada oficial **sem** `-SkipBuild` após mudanças no C# (Release desatualizado reproduz C1/M1).  
8. **Opcional (só validate):** `T-CMP-OPT-web-demo-flow`, `T-CMP-OPT-browser-demo-flow`.

## Relação com harnesses Rust-only

| Harness | Escopo |
|---------|--------|
| `eval-flow-control.ps1` | Rust slice 5 (24 PASS) |
| `eval-rpa-abc.ps1` | Rust slices 6–8 (27 PASS) |
| **`eval-grok-cross-stack.ps1`** | **Paridade Rust ↔ C#** nos exemplos compartilhados |

Rodar o cross-stack **depois** dos dois harnesses Rust quando validar o port C#.

## Contagem esperada de testes

O número exato de `T-CMP-V-*` depende de quantas pastas em `examples/` têm `manifest.json` (3 checks cada). Os IDs fixos acima somam **~25–40** linhas `GROK_EVAL` típicas. O bot deve usar **`fail=0`**, não um número fixo de PASS, salvo regressão documentada neste arquivo.

## Evidências

Salvar stdout completo em `docs/avaliacao/relatorios/<YYYY-MM-DD>/eval-cross-stack.txt` ao arquivar rodada Grok.
