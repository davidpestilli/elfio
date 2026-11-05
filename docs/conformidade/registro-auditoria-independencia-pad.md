# Registro de auditoria — independência de código-fonte do Power Automate Desktop (PAD)

Documento de **memória operacional** para análises futuras de plágio/cópia ilegal. Objetivo: registrar **o que já foi verificado** no repositório Elfio (implementação C# **Hoblio**) em relação ao estudo de engenharia reversa em `E:\Projetos\PAD-Engenharia-Reversa`.

**Última rodada (completa + incremental):** 2026-11-05 (UTC-3)  
**Commit Elfio de referência:** `6c999698271fc311cbb1da2f4983cc6fe38cc21e` — *Add PAD Variables module with demo, Studio panel, and gap docs.*  
**Baseline anterior:** `b0c0a7da` (primeira rodada completa no mesmo dia).

**Remediação Word (este commit):** removida em `WordContext.cs` a regex de intervalo de páginas **byte-a-byte igual** ao `ldstr` de `WordOperationsFacade` (PAD); substituída por `ParsePageRangeSpec` (parser próprio, mesma gramática `1, 3-5`).

---

## 1. Escopo e exclusões

### 1.1 Incluído (código e dados do produto)

| Área | Arquivos | Qtd. (2026-11-05 pós-módulos) |
|------|----------|----------------------------------|
| `src/Hoblio.Model` | `.cs` | 37 |
| `src/Hoblio.Runtime` | `.cs`, `.xaml` | 32 |
| `src/Hoblio.Serde` | `.cs` | 4 |
| `src/Hoblio.Cli` | `.cs` | 1 |
| `src/Hoblio.Studio` | `.cs`, `.xaml` (exceto `flow-canvas/`) | 70 |
| `src/Hoblio.Studio/flow-canvas/src` | `.ts`, `.tsx`, `.css` | 14 |
| `tests/**` | `.cs` | 31 |
| `scripts/**` | `.ps1`, `.py` | 7 |
| `examples/**` | `flow.json`, `manifest.json` | 30 |

**Total:** 226 artefatos de produto (sem `bin/`, `obj/`, `node_modules/`, `dist/`, `.hoblio/`).

### 1.2 Excluído

- Artefatos de build e dependências de terceiros (NuGet/npm).
- `docs/**` — revisão documental separada (§6); novos `docs/construcao/modulo-*.md` **sem** dumps IL.

### 1.3 Baseline PAD

- Repositório: `E:\Projetos\PAD-Engenharia-Reversa`
- Metodologia: [GUIA-METODOLOGIA-E-FERRAMENTAS.md](file:///E:/Projetos/PAD-Engenharia-Reversa/docs/GUIA-METODOLOGIA-E-FERRAMENTAS.md)
- Artefatos na comparação automatizada: **165** ficheiros (`*_decompiled*`, `*types-public*`, `*robin*symbols*`, `ActionFullNames*`)
- Pastas cruzadas na rodada incremental: `01-Automacao-Navegador`, `03-Variaveis`, `07-Excel`, `26-Word`

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

---

## 4. Resultados consolidados (estado atual)

| Verificação | Resultado |
|-------------|-----------|
| Referências `Microsoft.Flow` / `Robin` / `PAD` / `PowerAutomate` no código | **1** — comentário XML em `RunVariables.cs` (“alinhado ao PAD”); **não** é código Microsoft |
| Strings literais longas (≥24) do PAD no código | **0** (limitação: literais com `\` exigem varredura `ldstr`; ver abaixo) |
| Literais `ldstr` PAD (Word/Excel/Variáveis/Navegador, len≥20, excl. nomes de ação) vs Elfio | **0** após remediação da regex de páginas |
| Linhas normalizadas idênticas (C# vs IL) | **0** |
| Sobreposição de nome simples de **tipo** PAD vs tipos Elfio | **9** — apenas classes estáticas de enum/vocabulário em `VariablesEnums.cs` / `WordEnums.cs` (ex.: `MergeMode`, `InsertImageMode`), **não** classes de ação PAD |
| Ficheiros Elfio escaneados | **189** (`.cs`/`.tsx`/`.ts`/`.xaml`) |

**Conclusão:** não há indício de copiar-colar sistemático de código PAD. O único trecho identificado como **cópia literal** (regex de páginas Word) foi **substituído** nesta sessão de auditoria.

---

## 5. Nomenclatura alinhada ao vocabulário PAD (não é código copiado)

| Local | O quê |
|-------|--------|
| `ActionCatalog.cs`, `*ActionTypes.cs` (`ModuleActionIds`) | `action_id` / telemetria (ex.: `LaunchChromeBase`, `CreateNewDatatable`, `AttachToRunningWord`) |
| `*PaletteCatalog.cs` | Keywords de busca na paleta |
| `VariablesEnums.cs` | Comentários referenciando nomes PAD (`MergeMode.AddExtraColumns`, …) |
| `manifest.json` / `module_references` | Nomes de capacidade (`WebAutomation`, `Excel`, `Variables`, `Word`) |

Implementações distintas: PuppeteerSharp + CDP (browser); COM Excel/Word próprio; variáveis/datatable em JSON (`FlowTable`); **sem** Robin, `ModuleManager`, extensão CRX PAD.

---

## 6. Documentação (`docs/**`)

- Novos: `modulo-variaveis.md`, `modulo-word.md`, `modulo-excel.md`, `modulo-web-automation.md`, `notas-release-2026-11-05.md`.
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
| 08–25, 27–38 (exc. 29 parcial) | Não implementado ou só subconjunto anterior | N/A ou já coberto |

Demais linhas da rodada `b0c0a7da` (condicionais, loops, ficheiros, runtime grafo, etc.) **permanecem verificadas**; código base não revertido.

---

## 8. Próximas auditorias (incremental)

1. `git diff fb0caf7..HEAD --name-only -- src/ tests/ scripts/ examples/`
2. Reexecutar §9 (incluir bloco `ldstr` para módulos COM/regex).
3. Próximo baseline incremental: commit que inclui remediação Word + este registro (HEAD após merge).

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

Resultado esperado **após remediação Word:** `ldstr` hits **0** (podem persistir hits curtos de nomes de ação em `*ActionTypes.cs` — vocabulário intencional).

---

## 10. Histórico de rodadas

| Data | Commit | Notas |
|------|--------|-------|
| 2026-11-05 | `b0c0a7da` | Primeira varredura completa (178 artefatos) |
| 2026-11-05 | `6c99969` | Incremental: Variáveis, Word, WebAutomation modular, Excel/Studio; revarredura total 226 artefatos |
| 2026-11-05 | `fb0caf7` | Remediação regex Word + atualização deste registro |

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
