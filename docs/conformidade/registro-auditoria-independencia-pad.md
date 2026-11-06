# Registro de auditoria — independência de código-fonte do Power Automate Desktop (PAD)

Documento de **memória operacional** para análises futuras de plágio/cópia ilegal. Objetivo: registrar **o que já foi verificado** no repositório Elfio (implementação C# **Hoblio**) em relação ao estudo de engenharia reversa em `E:\Projetos\PAD-Engenharia-Reversa`.

**Última auditoria incremental:** 2026-11-06 (UTC-3)  
**Commit analisado (código):** `39eac80b2d6be84f3310c30da82e81f52f5add3a` — *Add HTTP REST, PDF, and LLM completion actions.*  
*(Conteúdo desta página formaliza essa rodada: contagens, diff `a15bf99..39eac80`, mapeamento de módulos. O hash Git do commit que altera apenas este ficheiro consta em `git log -- docs/conformidade/registro-auditoria-independencia-pad.md`.)*

**Baselines de diff úteis:**

| Intervalo | O quê |
|-----------|--------|
| `b0c0a7da` → `6c99969` | Primeiro grande incremento (Variáveis, Word, WebAutomation modular, …) |
| `a15bf99` → `39eac80` | **Última rodada formal** (HTTP/REST genérico, PDF PdfPig, LLM, composição de projetos) |
| `a15bf99` | Remediação Word: regex de páginas substituída por `ParsePageRangeSpec` (não copiar literal IL PAD) |

**Histórico breve:** 2026-11-05 — primeira varredura completa e módulos PAD-style; commit `a15bf99` removeu a única cópia literal confirmada (regex Word) e consolidou este registro.

---

## 1. Escopo e exclusões

### 1.1 Incluído (código e dados do produto)

| Área | Arquivos | Qtd. (HEAD `39eac80`, 2026-11-06) |
|------|----------|-------------------------------------|
| `src/Hoblio.Model` | `.cs` | 42 |
| `src/Hoblio.Runtime` | `.cs`, `.xaml` | 33 |
| `src/Hoblio.Serde` | `.cs` | 5 |
| `src/Hoblio.Cli` | `.cs` | 1 |
| `src/Hoblio.Studio` | `.cs`, `.xaml` (exceto `flow-canvas/`) | 75 |
| `src/Hoblio.Studio/flow-canvas/src` | `.ts`, `.tsx`, `.css` | 14 |
| `tests/**` | `.cs` | 37 |
| `scripts/**` | `.ps1`, `.py` | 7 |
| `examples/**` | `flow.json`, `manifest.json` | 40 |

**Total:** 254 artefatos de produto; **207** ficheiros `.cs`/`.tsx`/`.ts`/`.xaml` na varredura anti-plágio (sem `bin/`, `obj/`, `node_modules/`, `dist/`, `.hoblio/`).

### 1.2 Excluído

- Artefatos de build e dependências de terceiros (NuGet/npm).
- `docs/**` — revisão documental separada (§6); novos `docs/construcao/modulo-*.md` **sem** dumps IL.

### 1.3 Baseline PAD

- Repositório: `E:\Projetos\PAD-Engenharia-Reversa`
- Metodologia: [GUIA-METODOLOGIA-E-FERRAMENTAS.md](file:///E:/Projetos/PAD-Engenharia-Reversa/docs/GUIA-METODOLOGIA-E-FERRAMENTAS.md)
- Artefatos na comparação automatizada: **165** ficheiros (`*_decompiled*`, `*types-public*`, `*robin*symbols*`, `ActionFullNames*`)
- Pastas cruzadas em auditorias anteriores: `01-Automacao-Navegador`, `03-Variaveis`, `07-Excel`, `26-Word`; rodada 2026-11-06 também considerou `17-PDF`, `20-HTTP-Web` e `18-Runtime-e-Modulos-SDK` (composição) pelo **tipo** de funcionalidade nova

---

## 2. Metodologia

1. Referências a `Microsoft.Flow`, `Robin.`, `PAD.`, `PowerAutomate` no código.
2. Literais `"…"` longos (≥24) extraídos dos artefactos PAD vs código Elfio.
3. Linhas normalizadas (≥20 chars, sem comentários/IL) — interseção C# Elfio vs dumps `_decompiled`.
4. Nomes simples de tipos públicos PAD vs tipos declarados no Elfio.
5. **`ldstr` IL (rodada 2026-11-05 b):** literais em dumps Word/Excel/Variáveis/Navegador com escape; comparação exata no código (deteta regex e strings com `\`).
6. Revisão manual dos **novos sistemas** (§3).

---

## 3. Novos sistemas verificados (`b0c0a7da` → `6c99969`)

| Sistema | Ficheiros principais | Baseline PAD | Resultado |
|---------|----------------------|--------------|-----------|
| **Módulo Variáveis (PAD-style)** | `VariablesAction*.cs`, `VariablesContext.cs`, `FlowTable.cs`, `VariablesSemantics.cs`, `VariablesFilterConditions.cs`, Studio `Variables*` | `03-Variaveis` | **Sem** linhas/strings IL idênticas; implementação JSON + `FlowTable` própria |
| **Módulo Word** | `WordAction*.cs`, `WordContext.cs`, Studio `Word*` | `26-Word` | **Achado e corrigido:** regex de páginas copiada do IL de `WordOperationsFacade` (ver cabeçalho); restante COM wd* local |
| **Web Automation expandido** | `WebAutomationAction*.cs`, `BrowserContext.WebAutomation.cs`, `ChromiumAttach.cs`, `ChromiumPageSelection.cs`, `Win32Foreground.cs` | `01-Automacao-Navegador` | **Sem** literais longos/IL; attach via CDP/debug port, não extensão PAD |
| **Excel (camada modular)** | Refactor `ExcelAction*` + runtime existente | `07-Excel` | **Sem** literais `ldstr` suspeitos (len≥20) |
| **Diálogo lista** | `SelectFromListDialog.*`, `SelectFromListItems.cs` | `29-Caixas-Mensagem` (parcial) | Verificado no diff incremental |
| **Demos** | `variables-demo-flow`, `word-demo-flow`, `excel-demo-flow` | — | Apenas JSON/manifest |

**Diff incremental analisado:** 84 ficheiros `.cs` + 4 `.xaml` em `src/` e `tests/`; 0 strings longas PAD, 0 linhas normalizadas idênticas.

### 3.1 Rodada 2026-11-06 (`a15bf99` → `39eac80`)

| Sistema | Ficheiros principais | Baseline PAD (estudo) | Resultado |
|---------|----------------------|------------------------|-----------|
| **HTTP / REST** (`http_request`, extensão de `http_get`) | `ActionKind`, `ActionExecutor`, `ActionCatalog`, testes `HttpRequest*` | `20-HTTP-Web` | **Sem** `ldstr`/linhas IL coincidentes; `HttpClient` / JSON genérico |
| **LLM (completion)** | `LlmCompletionAction`, executor, testes `LlmCompletion*` | *(não mapeado 1:1 em folder)* | **Sem** strings longas PAD; POST + corpo JSON (MIME `application/json` — literal também comum em qualquer cliente HTTP, **não** evidência de cópia) |
| **PDF** | `PdfContext` (**UglyToad.PdfPig**), `PdfAction*`, paleta Studio, testes `Pdf*` | `17-PDF` | **Sem** cópia de IL; dependência NuGet OSS |
| **Composição / superfluxos** | `LinkedProjectValidator`, `ChildFlowResolver`, `ProjectLoader`, `LinkedProjectOps`, testes | `02`, `18` (conceito fluxo/manifest) | Lógica **JSON/manifest** própria; 0 linhas vs `FlowGraphBuilder` IL |

**Diff:** 44 paths em `src/` + `tests/` (`.cs`/`.xaml`); 25 paths em `docs/`, `examples/`, `scripts/` (sem impacto em copiar-colar de código PAD).

---

## 4. Resultados consolidados (estado atual)

| Verificação | Resultado |
|-------------|-----------|
| Referências `Microsoft.Flow` / `Robin` / `PAD` / `PowerAutomate` no código | **1** — comentário XML em `RunVariables.cs` (“alinhado ao PAD”); **não** é código Microsoft |
| Strings literais longas (≥24) do PAD no código | **0** (limitação: literais com `\` exigem varredura `ldstr`; ver abaixo) |
| Literais `ldstr` PAD (dumps `_decompiled`, len≥20) vs Elfio completo | **0** (HEAD `39eac80`) |
| Literais `ldstr` no diff `a15bf99..39eac80` (ficheiros `.cs` tocados, len≥16) vs PAD | **1** — `application/json` (MIME padrão; não específico de PAD) |
| Literais `ldstr` PAD vs Elfio (incremento 2026-11-06, len≥20) | **0** |
| Linhas normalizadas idênticas (C# vs IL) — repo completo e diff incremental | **0** |
| Sobreposição de nome simples de **tipo** PAD vs tipos Elfio | **9** — apenas classes estáticas de enum/vocabulário em `VariablesEnums.cs` / `WordEnums.cs` (ex.: `MergeMode`, `InsertImageMode`), **não** classes de ação PAD |
| Ficheiros Elfio escaneados | **207** (`.cs`/`.tsx`/`.ts`/`.xaml`) |

**Conclusão:** não há indício de copiar-colar sistemático de código PAD. O único trecho já identificado como **cópia literal** (regex de páginas Word) foi **removido** em `a15bf99`. O incremento HTTP/PDF/LLM/superfluxos **não** acrescentou novas correspondências com IL ou literais longos suspeitos.

---

## 5. Nomenclatura alinhada ao vocabulário PAD (não é código copiado)

| Local | O quê |
|-------|--------|
| `ActionCatalog.cs`, `*ActionTypes.cs` (`ModuleActionIds`) | `action_id` / telemetria (ex.: `LaunchChromeBase`, `CreateNewDatatable`, `AttachToRunningWord`) |
| `*PaletteCatalog.cs` | Keywords de busca na paleta |
| `VariablesEnums.cs` | Comentários referenciando nomes PAD (`MergeMode.AddExtraColumns`, …) |
| `manifest.json` / `module_references` | Nomes de capacidade (`WebAutomation`, `Excel`, `Variables`, `Word`) |
| `PdfActionTypes.cs` (`ModuleActionIds`) | Ex.: `ExtractTextFromPDF`, `MergePDFFiles` — rótulos alinhados ao grupo PDF PAD |

Implementações distintas: PuppeteerSharp + CDP (browser); COM Excel/Word próprio; variáveis/datatable em JSON (`FlowTable`); PDF via **PdfPig**; HTTP/LLM via **`HttpClient`**; **sem** Robin, `ModuleManager`, extensão CRX PAD.

---

## 6. Documentação (`docs/**`)

- Novos (até `39eac80`): `modulo-variaveis.md`, `modulo-word.md`, `modulo-excel.md`, `modulo-web-automation.md`, `modulo-http.md`, `modulo-pdf.md`, `modulo-llm.md`, `composicao-superfluxos.md`, `fluxo-eproc-peticionamento.md`, `notas-release-2026-11-05.md`.
- **Verificado:** sem linhas `IL_xxxx:`, sem anexos de decompilação PAD.
- Menções a gaps/paridade PAD são **produto**, não código incorporado.

---

## 7. Mapeamento PAD (grupos do estudo) × Elfio

| Grupo PAD | Presença Elfio | Auditoria |
|-----------|----------------|-----------|
| 01 Automacao-Navegador | `WebAutomationAction*`, `BrowserContext` (+ partial legado `browser_*`) | Verificado — stack CDP |
| 03 Variaveis | `variables_*`, `FlowTable`, listas | Verificado |
| 07 Excel | `excel_*`, `ExcelContext` | Verificado |
| 26 Word | `word_*`, `WordContext` | Verificado + regex remediada |
| 17 PDF | `pdf_*`, `PdfContext` (PdfPig) | Verificado (`39eac80`) |
| 20 HTTP-Web | `http_get`, `http_request`, LLM POST | Verificado (`39eac80`) — não é o mesmo pipeline que módulos REST/SOAP PAD |
| 18 Runtime/manifest | `linked_projects`, `ChildFlowResolver` | Verificado (`39eac80`) |
| 08–16, 19–23, 25, 27–38 (exc. cobertos acima) | Parcial ou não | N/A ou já coberto em rodadas anteriores |

Demais linhas da rodada `b0c0a7da` (condicionais, loops, ficheiros, runtime grafo, etc.) **permanecem verificadas**; código base não revertido.

---

## 8. Próximas auditorias (incremental)

1. `git diff 39eac80..HEAD --name-only -- src/ tests/ scripts/ examples/`
2. Reexecutar §9 (incluir bloco `ldstr` para módulos COM/regex e novos módulos HTTP/PDF).
3. Se o diff apenas adicionar vocabulário `action_id` / docs, revisão manual rápida basta; se houver parsing pesado (regex, protocolo binário), reforçar passo `ldstr`.

---

## 9. Script reprodutível (PowerShell)

```powershell
$elfioRoot = "E:\Projetos\elfio"
$padRoot   = "E:\Projetos\PAD-Engenharia-Reversa"

$elfioFiles = Get-ChildItem -Path $elfioRoot -Recurse -Include *.cs,*.tsx,*.ts,*.xaml -File |
  Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|dist|\.hoblio)\\' }

$elfioText = ($elfioFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"

$padFiles = Get-ChildItem -Path $padRoot -Recurse -Include *.txt -File |
  Where-Object { $_.FullName -match '_decompiled' }

$ldstrHits = @()
foreach ($f in $padFiles) {
  $raw = Get-Content $f.FullName -Raw
  if (-not $raw) { continue }
  foreach ($m in [regex]::Matches($raw, 'ldstr\s::\s*"((?:[^"\\]|\\.)*)"')) {
    $lit = $m.Groups[1].Value
    if ($lit.Length -ge 20 -and $elfioText.Contains($lit)) { $ldstrHits += $lit }
  }
}

$msHits = Select-String -Path ($elfioFiles.FullName) -Pattern 'Microsoft\.Flow|Robin\.|PAD\.|PowerAutomate' -ErrorAction SilentlyContinue

Write-Host "Elfio files: $($elfioFiles.Count)"
Write-Host "ldstr literal hits (len>=20): $($ldstrHits.Count)"
Write-Host "Microsoft.Flow/Robin/PAD refs: $($msHits.Count)"
if ($ldstrHits) { $ldstrHits | Sort-Object -Unique }
```

Resultado esperado no **repo completo:** `ldstr` hits len≥20 **0** (podem surgir literais curtos genéricos como `application/json` ao comparar só o diff — filtrar por contexto). Nomes de ação em `*ActionTypes.cs` são vocabulário intencional, não cópia de implementação.

---

## 10. Histórico de rodadas

| Data | Commit | Notas |
|------|--------|-------|
| 2026-11-05 | `b0c0a7da` | Primeira varredura completa (178 artefatos) |
| 2026-11-05 | `6c99969` | Incremental: Variáveis, Word, WebAutomation modular, Excel/Studio; revarredura total 226 artefatos |
| 2026-11-05 | `a15bf99` | Remediação regex Word + atualização deste registro |
| 2026-11-05 | `c1d2e1f` | Registo do hash `a15bf99` no histórico deste documento |
| 2026-11-06 | `39eac80` | HTTP REST, PDF (PdfPig), LLM completion, composição de projetos; auditoria incremental `a15bf99..39eac80` sem cópia IL |
| 2026-11-06 | *(doc)* | Commit seguinte: formalização deste registro para a rodada `39eac80` |

---

## 11. Anexo — ficheiros tocados no incremento `b0c0a7da..6c99969`

<details>
<summary>Lista (88 paths em src/tests — cs/xaml)</summary>

- **Model:** `ActionCatalog.cs`, `ActionKind.cs`, `ExcelAction*.cs`, `VariablesAction*.cs`, `WebAutomationAction*.cs`, `WordAction*.cs`, `Json/*ActionJson.cs`, `GraphValidator.cs`, `TemplateReferenceValidator.cs`, …
- **Runtime:** `ActionExecutor.cs`, `BrowserContext*.cs`, `Chromium*.cs`, `Excel*.cs`, `FlowTable.cs`, `Variables*.cs`, `WordContext.cs`, `Win32Foreground.cs`, `SelectFromListDialog.*`, …
- **Studio:** `*PaletteCatalog.cs`, `*ActionDefaults.cs`, `VariablesActionUi.cs`, `ViewModels/FlowEditorViewModel.*Fields*.cs`, `PropertiesPanel.xaml`, …
- **Tests:** `*CoverageTests.cs`, `VariablesBehaviorTests.cs`, `ChromiumPageSelectionTests.cs`, `PaletteCatalogStructureTests.cs`, …

Gerar lista atualizada: `git diff b0c0a7da..6c99969 --name-only -- src/ tests/`

</details>

<details>
<summary>Incremento <code>a15bf99</code> → <code>39eac80</code> (2026-11-06)</summary>

- **Diff código:** `git diff a15bf99..39eac80 --name-only -- src/ tests/` → 44 paths (novos: `PdfContext`, `PdfAction*`, `LinkedProjectValidator`, `LinkedProjectOps`, testes HTTP/PDF/LLM/superflow, …).
- **Diff docs/exemplos/scripts:** 25 paths (`modulo-http.md`, `modulo-pdf.md`, `modulo-llm.md`, `composicao-superfluxos.md`, exemplos `superflow-*`, `eproc-*`, …).

</details>
