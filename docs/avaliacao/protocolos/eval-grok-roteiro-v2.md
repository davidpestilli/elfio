# Roteiro Grokbot v2 — paridade Rust ↔ C# (pós-correções)

**Versão:** 2026-10-27 (após fixes de browser, CLI, testes e Studio)  
**Harness base:** `scripts/eval-grok-cross-stack.ps1`  
**Protocolo de IDs:** `eval-grok-cross-stack.md`  
**Relatório anterior (baseline de gaps):** `reports/eval-cross-stack-2026-10-27/RELATORIO_PARIDADE_RUST_CSHARP.md`

Este roteiro substitui a **seção 2** do relatório de 27/10 (manhã). O bot deve seguir as fases **na ordem** e só emitir veredito final na **Fase 7**.

---

## 0. Contexto — o que mudou desde a rodada REPROVADA

| ID antigo | Problema | Correção esperada agora |
|-----------|----------|-------------------------|
| **C1** | `browser_navigate` → `Invalid referrerPolicy` (PuppeteerSharp 20 × Chrome 155) | PuppeteerSharp **25.15.0**, navegação com `ReferrerPolicy` compatível; `browser-demo-flow` roda no CLI |
| **M1** | `--jsonl` gravava `{}` | `RunEventJson.Serialize` com payload Studio-shaped |
| **M2** | CLI com `Unhandled exception` + stack | `Error:` + `Caused by:` (`RunErrors` / `HoblioRunException`); stack só com `HOBLIO_DEBUG=1` |
| **B1** | increase/decrease não numérico sem contexto | Mensagem estilo Rust + cadeia de causa |
| **B3** | `file_write` contava caracteres como bytes | Contagem **UTF-8** |
| **A1 (parcial)** | Studio “só visualizador” | Palette (20 ações), `PropertiesPanel`, undo/**refazer**, log, Stop, variáveis JSON, arestas, destaque de nó em execução |
| **M3 (parcial)** | 6 testes vs ~24 Rust | **27** testes (`ActionKindTests`, `UnitBehaviorTests` expandido, demos, browser opcional) |

**Ainda fora de paridade total (não bloqueiam harness v2, mas entram no veredito “produto Studio”):**

- Waypoints / `WaypointEdge` (Nodify)
- Reconectar arestas arrastando no canvas
- Modo `-StrictSummaryLines` (contagem de linhas Rust ≠ C# por design de logging)
- `node_enter` / `run_started` na CLI C# (Rust emite tracing duplicado)

---

## 1. Objetivos de veredito (três níveis)

O Grokbot deve declarar **três** vereditos separados:

| Nível | Pergunta | Critério de **APROVADO** |
|-------|----------|---------------------------|
| **L1 — Núcleo** | Modelo + validate + run offline batem com Rust? | Fase 1 + Fase 2 + Fase 4 (probes offline) → `fail=0` |
| **L2 — Browser + CLI contrato** | RPA web e ferramentas de integração OK? | L1 + Fase 3 + checks T-CLI-* PASS |
| **L3 — Studio editor** | Editor WPF ≈ React Flow? | Checklist Fase 5: itens **obrigatórios** PASS; waypoints = conhecido pendente |

**REPROVADO global** se L1 falhar. **APROVADO parcial** se L1+L2 passam e L3 tem gaps documentados.

---

## 2. Pré-requisitos (Fase 0)

1. Caminhos (ajustar se necessário):

   | Stack | Padrão |
   |-------|--------|
   | Rust | `E:\Projetos\hoblio` |
   | C# | `E:\Projetos\hoblio-c#` |
   | Examples | `E:\Projetos\hoblio\examples` (junção em `hoblio-c#\examples`) |

2. **Rust:** `git status --porcelain` → registrar; **não** commitar/push sem pedido do usuário. Harness não deve alterar arquivos tracked.
3. **Chrome** instalado (para Fase 3). Sem Chrome: marcar T-BRW-* como `SKIP` com motivo, não inventar PASS.
4. Criar pasta de evidências:  
   `reports/eval-cross-stack-<YYYY-MM-DD>/evidencias/`  
   (reutilizar probes em `.hoblio-eval-temp\grok-probes\` se ainda existirem).

5. Ler `docs/PARITY.md` (C#) — checklist vivo de Studio/testes.

---

## 3. Fase 1 — Harness cross-stack (obrigatório)

```powershell
cd E:\Projetos\hoblio-c#
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\eval-grok-cross-stack.ps1 `
  2>&1 | Tee-Object -FilePath reports\eval-cross-stack-<DATA>\eval-cross-stack.log
```

**Sucesso:** exit **0**, `GROK_EVAL: SUMMARY pass=<n> fail=0`.

Inclui (resumo):

- P1–P6, T-B01, T-B02  
- **T-C01** — `dotnet test` Release  
- **T-C01-MIN** — total de testes aprovados ≥ **27**  
- T-CMP-V-* (todos `examples/*` com `manifest.json`)  
- T-CMP-E01..E06, E05-REPORT, T-CMP-N01-*  
- T-CMP-OPT-web / browser (validate only)  
- **Fase 1b no script:** T-CLI-*, T-BRW-UNIT, T-CMP-E07 (browser run)

**Opcional informativo** (não bloqueia L1):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\eval-grok-cross-stack.ps1 -SkipBuild -StrictSummaryLines
```

Esperado: falhas em T-CMP-E0* por contagem de linhas — documentar como **INFO**, não regressão.

---

## 4. Fase 2 — Contrato CLI C# (regressão M1/M2/B1/B3)

Executado pelo script (`T-CLI-*`). Validação manual se o script falhar:

| TEST_ID | Comando / ação | PASS se |
|---------|----------------|---------|
| **T-CLI-J01** | `hoblio run <demo-flow> --jsonl` | Última linha de `.hoblio/run.jsonl` contém `action_executed` e **não** é `{}` |
| **T-CLI-V01** | `hoblio validate <demo-flow>` | Exit 0; **stderr** contém `valid:` |
| **T-CLI-E01** | Probe increase não numérico (pasta temp) | Exit ≠ 0; stderr contém `Error:` e `Caused by:` (mensagem B1: `must be numeric for increase/decrease`) |
| **T-CLI-E01-STRICT** | (informativo) | Mesmo probe **sem** linha `Unhandled exception` após `Caused by:` — hoje pode **FAIL** por rethrow do System.CommandLine; não bloqueia L1 se E01 passar |
| **T-CLI-E02** | Mesmo probe no Rust | Mesma semântica (exit ≠ 0, mensagem numérica) |
| **T-CLI-F01** | `files-lists-demo` run | Resumo `file_write`/`file_append` usa contagem coerente com bytes UTF-8 (+N bytes) |

Salvar logs: `evidencias/cli-jsonl.log`, `cli-validate-stderr.log`, `cli-neg-increase.log`.

---

## 5. Fase 3 — Browser (regressão C1)

| TEST_ID | Ação | PASS se |
|---------|------|---------|
| **T-BRW-UNIT** | `HOBLIO_BROWSER_TEST=1` + `dotnet test --filter Browser_navigate_local_html` | Teste passa (ou SKIP explícito se env não setado **e** bot acordou pular) |
| **T-CMP-E07** | `hoblio run examples/browser-demo-flow` Rust **e** C# | Exit 0 ambos; saída contém marcador de navegação (`example.com` ou título esperado do demo) |
| **T-BRW-PROBE-SC02** | CLI run probe `sc02-click-nav` (cs + rust) | Exit 0 ambos (logs em `grok-probes`) |
| **T-BRW-PROBE-SC03** | CLI run probe `sc03-wait-nav` | Exit 0 ambos |

Se **E07** falhar no C# com `referrerPolicy` ou `NavigationException` → **L2 REPROVADO** (regressão C1).

---

## 6. Fase 4 — Probes estruturados (21 cenários)

Reutilizar matriz da rodada anterior (`reports/eval-cross-stack-2026-10-27/evidencias/probe-*`).

Para cada par `probe-rust-*.log` / `probe-cs-*.log`:

1. Comparar **exit code**.  
2. Se exit 0, comparar substrings obrigatórias (tabelas sa02–sc05, neg-*).  
3. Emitir linha agregada:  
   `GROK_EVAL: T-PROBE-<nome> PASS|FAIL <rust_exit>/<cs_exit>`

**Offline (sem browser):** todos os `neg-*`, `sa02`–`sa05`, `sb01`, `sb02`, `sc01`, `sc04`, `sc05` devem continuar **PASS**.

---

## 7. Fase 5 — Studio WPF (checklist manual)

Abrir **`Hoblio.Studio`** (Release ou `dotnet run`). Usar cópia de `flow-control-demo` ou projeto já aberto.

Marcar cada item **PASS / FAIL / N/A** no relatório:

| ID | Item | Referência Rust | Critério PASS |
|----|------|-----------------|---------------|
| **S01** | Abrir / salvar projeto | Tauri load/save | Abre pasta com `manifest.json`; salvar persiste `flow.json` |
| **S02** | Validar / executar | validate/run IPC | Toolbar valida e executa; log preenche |
| **S03** | Stop | cancel_run | Parar durante delay longo cancela run |
| **S04** | Palette 20 ações + controles | defaultAction | Adicionar nó `browser_fill`, `if`, etc. |
| **S05** | PropertiesPanel | PropertiesPanel.tsx | Trocar tipo de ação; editar campos; if/for_each/while/repeat |
| **S06** | Arestas | edge + port | Criar/remover aresta; editar porta then/else/body |
| **S07** | Variáveis projeto | variables JSON | Editor JSON altera manifest |
| **S08** | Undo / refazer | useFlowEditor history | Desfazer adição de nó restaura grafo |
| **S09** | Dirty / fechar | unsaved warning | `*` no título; aviso ao fechar |
| **S10** | Destaque execução | activeNodeId | Nó atual destacado durante run |
| **S11** | Última exec. / produtores | last run vars | Painel mostra variáveis após run |
| **S12** | Waypoints | WaypointEdge | **PENDENTE** — FAIL esperado até implementar |

**L3 APROVADO** se S01–S11 PASS e S12 documentado como gap.

---

## 8. Fase 6 — Análise estática (amostragem)

Comparar rapidamente (grep/leitura, sem reimplementar):

- `crates/hoblio-model/src/action.rs` ↔ `Hoblio.Model/ActionKind.cs` (20 tipos)  
- `GraphValidator` mensagens-chave (`duplicate outgoing port`, `retry_count`)  
- Versão PuppeteerSharp em `Hoblio.Runtime.csproj` (**≥ 25.15.0**)

Registrar divergências **novas** não cobertas por testes.

---

## 9. Fase 7 — Relatório final

Gerar `reports/eval-cross-stack-<DATA>/RELATORIO_PARIDADE_RUST_CSHARP.md` com:

1. **Vereditos L1 / L2 / L3** (tabela §1).  
2. **SUMMARY** do harness (`pass` / `fail`).  
3. **Regressões** vs relatório 27/10 manhã (C1, M1, M2, A1, M3).  
4. **Gaps remanescentes** (waypoints, strict logs, CLI tracing).  
5. **Evidências** (lista de arquivos `.log`).  
6. **Estado git Rust** (before/after) — confirmar zero alterações indesejadas.

Template de veredito one-liner:

```text
L1: APROVADO | REPROVADO — <motivo>
L2: APROVADO | REPROVADO | SKIP(browser) — <motivo>
L3: APROVADO | PARCIAL — S12 pendente; demais S01–S11: <n>/11 PASS
```

---

## 10. Ordem resumida (copiar para prompt Grok)

1. Fase 0 — paths, git status, pasta evidências  
2. Fase 1 — `eval-grok-cross-stack.ps1` → fail=0  
3. Fase 2 — conferir T-CLI-* no log  
4. Fase 3 — browser unit + E07 + probes sc02/sc03  
5. Fase 4 — probes 21 cenários  
6. Fase 5 — checklist Studio S01–S12  
7. Fase 6 — amostragem estática  
8. Fase 7 — relatório markdown + vereditos L1–L3  

**Não** tratar `-StrictSummaryLines` como gate de release.

---

## 11. Contagem esperada de `GROK_EVAL`

| Bloco | ~linhas |
|-------|---------|
| P + build + test | 9 |
| T-CMP-V (9 exemplos × 3) | 27 |
| E01–E07 + E05-REPORT | 8 |
| N01 | 3 |
| OPT | 2 |
| T-CLI + T-BRW | 6–8 |
| **Total típico** | **~55–60 PASS** |

Use **`fail=0`**, não número fixo de PASS, se novos exemplos forem adicionados.
