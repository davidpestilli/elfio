# Registro de auditoria — independência de código-fonte do Power Automate Desktop (PAD)

Documento de **memória operacional** para análises futuras de plágio/cópia ilegal. Objetivo: registrar **o que já foi verificado** no repositório Elfio (implementação C# **Hoblio**) em relação ao estudo de engenharia reversa em `E:\Projetos\PAD-Engenharia-Reversa`.

**Última rodada completa:** 2026-11-05 (UTC-3)  
**Commit Elfio analisado:** `b0c0a7da7323b295de1cccbd711885dbcc0543cd` — *Add Switch/Case controls, fix save stack overflow, and document nov/2026 round.*

---

## 1. Escopo e exclusões

### 1.1 Incluído (código e dados do produto)

| Área | Arquivos | Qtd. |
|------|----------|------|
| `src/Hoblio.Model` | `.cs` | 21 |
| `src/Hoblio.Runtime` | `.cs`, diálogos WPF | 21 |
| `src/Hoblio.Serde` | `.cs` | 4 |
| `src/Hoblio.Cli` | `.cs` | 1 |
| `src/Hoblio.Studio` | `.cs`, `.xaml` (exceto `flow-canvas/`) | 59 |
| `src/Hoblio.Studio/flow-canvas/src` | `.ts`, `.tsx`, `.css` | 14 |
| `tests/**` | `.cs` | 25 |
| `scripts/**` | `.ps1`, `.py` | 7 |
| `examples/**` | `flow.json`, `manifest.json` | 26 |

**Total:** 178 artefatos de produto (sem contar `bin/`, `obj/`, `node_modules/`, `dist/`, `.hoblio/` de runs locais).

### 1.2 Excluído (não requer revarredura de plágio PAD)

- Artefatos de build (`bin/`, `obj/`, `flow-canvas/dist/`, WebView2 em runtime).
- Dependências de terceiros (`node_modules/`, pacotes NuGet resolvidos no cache global).
- Documentação em `docs/**` — **revisão separada** (§6): textos podem mencionar PAD como referência de produto; não contêm dumps de IL nem trechos de código PAD colados.

### 1.3 Baseline PAD (comparação)

- Repositório: `E:\Projetos\PAD-Engenharia-Reversa`
- Metodologia PAD: [GUIA-METODOLOGIA-E-FERRAMENTAS.md](file:///E:/Projetos/PAD-Engenharia-Reversa/docs/GUIA-METODOLOGIA-E-FERRAMENTAS.md)
- Artefatos usados na comparação automatizada:
  - **155** ficheiros em `*_decompiled*`, `*types-public*`, `*robin*symbols*`, `ActionFullNames*`
  - Documentação markdown dos grupos `01`–`38` (vocabulário e nomes públicos de ações)

Versão PAD referenciada no estudo: `Microsoft.PowerAutomateDesktop_11.2610.155.0_x64` (ver guia PAD).

---

## 2. Metodologia aplicada nesta rodada

1. **Referências explícitas a assemblies PAD** — busca por `Microsoft.Flow`, `Robin.`, `PAD.`, `PowerAutomate` em `.cs`, `.tsx`, `.ts`, `.xaml`, `.csproj`, `.sln`, `.json` (exceto docs).
2. **Strings longas idênticas** — literais `"…"` com ≥24 caracteres extraídos dos dumps PAD; busca exata no texto concatenado do código Elfio (147 ficheiros `.cs`/`.tsx`/`.ts`/`.xaml` na varredura principal).
3. **Linhas de código normalizadas** — linhas com ≥20 caracteres, trim + colapso de espaços, exclusão de comentários e prefixos IL (`IL_`, `.method`); interseção entre **todo** `.cs` em `src/` e `tests/` vs ficheiros `*_decompiled*.txt` do PAD.
4. **Nomes de tipos públicos PAD** — simple names de ~1099 tipos em `*types-public.txt` vs ~203 tipos declarados no Elfio (`class`/`record`/`struct`/`interface`/`enum`).
5. **Revisão manual dirigida** — runtime de browser (PuppeteerSharp vs extensão/native messaging PAD), Excel COM, catálogo `module`/`action_id`, executor de grafo, canvas React Flow vs `FlowGraphBuilder` PAD.

---

## 3. Resultados consolidados

| Verificação | Resultado |
|-------------|-----------|
| Referências `Microsoft.Flow` / `Robin` / `PAD` no código-fonte Elfio | **0** ocorrências |
| Referências PAD em `.csproj` / manifests de build | **0** |
| Strings literais longas (≥24) do PAD presentes no código Elfio | **0** de 1370 candidatos |
| Linhas normalizadas idênticas (código vs dumps IL decompilados) | **0** |
| Sobreposição de **nome simples** de tipo público PAD com tipos Elfio | **0** de 1099 |
| Strings longas do canvas TS vs artefactos browser PAD (`_decompiled`, extensões) | **0** |
| Linhas `IL_xxxx:` em documentação Elfio | **0** |

**Conclusão desta rodada:** não foi encontrado indício de **copiar-colar de código-fonte** do PAD (C#, IL decompilado ou extensões de browser) na implementação Elfio/Hoblio analisada.

---

## 4. Nomenclatura alinhada ao vocabulário PAD (não é código copiado)

Há **identificadores de telemetria e manifest** que reutilizam **nomes públicos** documentados no estudo PAD (classes de ação Robin / `systemActionName`), sem importar DLLs Microsoft:

| Local | O quê | Risco legal / plágio |
|-------|--------|----------------------|
| `src/Hoblio.Model/ActionCatalog.cs` | `module` + `action_id` (ex.: `WebAutomation`, `LaunchChromeBase`, `PopulateTextFieldBase`) | Vocabulário de **metadados**; implementação é PuppeteerSharp + JSON próprio |
| `src/Hoblio.Model/ExcelActionTypes.cs` | `ModuleActionIds` (ex.: `LaunchExcel`, `AttachToRunningExcel`, `ReadFromExcel`) | Mesmos **rótulos** que símbolos Robin; tipos JSON Elfio são `excel_*` |
| `src/Hoblio.Model/WordActionTypes.cs` | `ModuleActionIds` (ex.: `LaunchWord`, `AttachToRunningWord`, `ReadFromWord`) | Mesmos **rótulos** que símbolos Robin; tipos JSON Elfio são `word_*` |
| `docs/construcao/catalogo-acoes-e-modulos.md` | Tabela `type` → `module` / `action_id` | Documentação de produto |
| `examples/*/manifest.json` | `module_references[].name` (ex.: `WebAutomation`) | Capacidades declaradas, não binários PAD |
| `src/Hoblio.Studio/ExcelPaletteCatalog.cs` | Texto de busca da paleta inclui nomes PAD (ex.: `AttachToRunningExcel`) | Apenas **keywords** de UI |
| `src/Hoblio.Studio/WordPaletteCatalog.cs` | Keywords PAD (ex.: `LaunchWord`, `InsertImageToWord`) | Apenas **keywords** de UI |

**Implementações distintas verificadas:**

- **Browser:** `BrowserContext.cs` — PuppeteerSharp/CDP; PAD usa extensão + native messaging + runtime `WebAutomationRuntimeServiceProxy` (ver pasta `01-Automacao-Navegador` no estudo PAD).
- **Excel:** `ExcelContext.cs` + `ExcelComSemantics.cs` — COM `dynamic` + constantes xl* locais; não há cópia de IL das classes `Microsoft.Flow.RPA.Desktop.Modules.Excel.Actions.*`.
- **Word:** `WordContext.cs` — COM `dynamic` + constantes wd* locais; não há cópia de IL das classes `Microsoft.Flow.RPA.Desktop.Modules.Word.Actions.*`.
- **Fluxo:** `FlowExecutor.cs` + JSON `flow.json`; PAD usa Robin, `FlowGraphBuilder`, `Robin.Engine` (pastas `02`, `18`, `19` no estudo PAD) — **sem parser Robin** no Elfio.

Se a política interna exigir **distanciamento nominal total**, alterar apenas `ActionCatalog` / `ExcelActionTypes` / manifests (breaking change de telemetria), **não** reimplementar runtime.

---

## 5. Mapeamento por domínio PAD (grupos do estudo) × Elfio

Legenda: **Verificado** = código Elfio existente comparado ao baseline PAD; **N/A** = funcionalidade ainda não implementada no Elfio (nada a copiar na árvore atual).

| Grupo PAD (pasta estudo) | Presença no Elfio | Status auditoria |
|--------------------------|-------------------|------------------|
| 01 Automacao-Navegador | `browser_*`, `ChromiumLocator`, `BrowserContext` | Verificado — stack diferente (PuppeteerSharp) |
| 02 Construcao-Fluxos-Grafos | Studio + `FlowExecutor`, `GraphValidator` | Verificado — sem IL/linhas comuns com `FlowGraphBuilder` |
| 03 Variaveis | `RunVariables`, `set_variable`, listas | Verificado |
| 04 Condicionais | If, Switch/Case | Verificado (`SwitchBehaviorTests`) |
| 05 Loops | ForEach, While, Repeat, break/continue | Verificado |
| 06 Controle-de-Fluxo | `run_flow`, Start/End | Verificado |
| 07 Excel | `excel_*`, `ExcelContext` | Verificado — COM próprio |
| 08 UI Automation | — | N/A |
| 09 Mouse-e-Teclado | — | N/A |
| 10 Area-de-Transferencia | — | N/A |
| 11 Texto | parcial (`ExpressionEvaluator`) | Verificado (escopo atual) |
| 12 Arquivos | `FileActions` | Verificado |
| 13 Imagens / 14 OCR | — | N/A |
| 15 Data-Hora | — | N/A |
| 16 Sistema | `log`, `delay`, `http_get` | Verificado (subconjunto) |
| 17 PDF | — | N/A |
| 18 Runtime-e-Modulos-SDK | `ActionExecutor`, telemetria | Verificado — sem `ModuleManager`/Robin |
| 19 Variaveis-e-Tipos-Runtime | `RunVariables`, expressões | Verificado |
| 20 HTTP-Web | `http_get` | Verificado |
| 21–23 FTP, Terminal, Filas | — | N/A |
| 25 Gravador | — | N/A |
| 26 Word | `word_*`, `WordContext` | Verificado — COM próprio |
| 27 Triggers … 38 Executar-Fluxo | — | N/A (exceto conceito `run_flow` já coberto) |
| 29 Caixas-Mensagem | `NativeDialogs`, WPF dialogs | Verificado |

---

## 6. Documentação (`docs/**`)

- **Escopo:** menções a “estilo PAD”, “PAD-style”, paridade de UX — **inspiração de produto**, não incorporação de código.
- **Verificado:** ausência de dumps IL, blocos de decompilação ou listagens extensas copiadas de `*_decompiled*` do repositório PAD dentro de `docs/`.
- **Não repetir em futuras rodadas** salvo se novos ficheiros markdown incluírem anexos de código de terceiros.

---

## 7. Inventário por ficheiro (checklist — rodada 2026-11-05)

Marcar novos ficheiros fora desta lista na próxima auditoria incremental.

<details>
<summary><code>src/Hoblio.Model</code> (21)</summary>

- `ActionCatalog.cs`, `ActionKind.cs`, `ControlKind.cs`, `ElfioEngineInfo.cs`, `Enums.cs`, `ErrorHandling.cs`
- `ExcelAction.cs`, `ExcelActionMutator.cs`, `ExcelActionTypes.cs`, `ExcelEnums.cs`
- `Flow.cs`, `GraphValidator.cs`, `ModuleReference.cs`, `Project.cs`, `SubflowBundle.cs`, `TemplateReferenceValidator.cs`
- `Json/ActionKindJsonConverter.cs`, `Json/ControlKindJsonConverter.cs`, `Json/ExcelActionJson.cs`, `Json/HoblioJson.cs`, `Json/SnakeCaseNamingPolicy.cs`

</details>

<details>
<summary><code>src/Hoblio.Runtime</code> (21)</summary>

- `ActionExecutor.cs`, `AdjustVariable.cs`, `BrowserContext.cs`, `ChromiumLocator.cs`, `ExcelComSemantics.cs`, `ExcelContext.cs`, `WordContext.cs`
- `ExpressionEvaluator.cs`, `FileActions.cs`, `FlowExecutor.cs`, `ListActions.cs`, `NativeDialogs.cs`
- `RunErrors.cs`, `RunEventJson.cs`, `RunEvents.cs`, `RunFlowSupport.cs`, `RunTelemetry.cs`, `RunVariables.cs`
- `SelectFromListDialog.xaml.cs`, `SelectFromListItems.cs`, `UserInputDialog.xaml.cs`, `GlobalUsings.cs`

</details>

<details>
<summary><code>src/Hoblio.Serde</code> (4)</summary>

- `ChildFlowResolver.cs`, `ModuleManifestSync.cs`, `ProjectFlowCatalog.cs`, `ProjectLoader.cs`

</details>

<details>
<summary><code>src/Hoblio.Cli</code> (1)</summary>

- `Program.cs`

</details>

<details>
<summary><code>src/Hoblio.Studio</code> — C# / XAML (59)</summary>

Ver listagem completa gerada na rodada (ActionPalette*, ExcelPalette*, FlowCanvas/*, ViewModels/*, painéis XAML, `StudioTheme.xaml`, etc.).

</details>

<details>
<summary><code>flow-canvas/src</code> (14)</summary>

- `App.tsx`, `bridge.ts`, `main.tsx`, `FlowToolbar.tsx`, `styles.css`, `useCanvasShortcuts.ts`
- `flowAlignment.ts`, `flowHandles.ts`, `flowLayout.ts`, `nodeLayout.ts`, `FlowAlignmentGuides.tsx`
- `edges/WaypointEdge.tsx`, `edges/waypointTypes.ts`, `nodes/FlowNode.tsx`

</details>

<details>
<summary><code>tests</code> (25) e <code>scripts</code> (7) e <code>examples</code> (26 JSON)</summary>

Listagem integral disponível no histórico da sessão de auditoria 2026-11-05 (comando de inventário PowerShell no repositório Elfio).

</details>

---

## 8. Próximas auditorias (incremental)

1. **`git diff b0c0a7da..HEAD --name-only`** — analisar apenas ficheiros novos/alterados em `src/`, `tests/`, `scripts/`, `examples/`.
2. Reexecutar o script da §9 nos ficheiros tocados.
3. Se adicionar módulo novo (UI Automation, OCR, Word, …), abrir linha correspondente na tabela §5 e marcar **Verificado**.
4. Ao colar snippets de estudo PAD em chats ou docs, **não** commitar no repo Elfio sem revalidar.

---

## 9. Script reprodutível (PowerShell)

Executar a partir de `E:\Projetos\elfio` (ajustar caminhos se necessário):

```powershell
$elfioRoot = "E:\Projetos\elfio"
$padRoot   = "E:\Projetos\PAD-Engenharia-Reversa"

$elfioFiles = Get-ChildItem -Path $elfioRoot -Recurse -Include *.cs,*.tsx,*.ts,*.xaml -File |
  Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|dist|\.hoblio)\\' }

$elfioText = ($elfioFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"

$padFiles = Get-ChildItem -Path $padRoot -Recurse -Include *.txt,*.md -File |
  Where-Object { $_.FullName -match '_decompiled|types-public|robin-.*symbols|ActionFullNames' }

$padText = ($padFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"

$padStrings = [regex]::Matches($padText, '"([^"\\]{24,})"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
$stringHits = $padStrings | Where-Object { $elfioText.Contains($_) }

$msHits = Select-String -Path ($elfioFiles.FullName) -Pattern 'Microsoft\.Flow|Robin\.|PAD\.|PowerAutomate' -ErrorAction SilentlyContinue

Write-Host "Elfio files: $($elfioFiles.Count)"
Write-Host "Long string hits: $($stringHits.Count)"
Write-Host "Microsoft.Flow/Robin/PAD refs: $($msHits.Count)"
```

Resultado esperado após rodada 2026-11-05: **0 / 0**.

---

## 10. Histórico de rodadas

| Data | Commit Elfio | Analista | Notas |
|------|--------------|----------|-------|
| 2026-11-05 | `b0c0a7da` | Auditoria automatizada + revisão manual (Cursor) | Primeira rodada completa registrada neste ficheiro |
