# Módulo Variáveis (`variables_*` + ações legadas)

Paleta **Variáveis** do PAD: tabelas de dados, listas, números, JSON e escalares. Telemetria: módulo **`Variables`**, `action_id` alinhado ao PAD (ex.: `CreateNewDatatable`, `AddItemToList`).

**Referência de engenharia reversa:** `PAD-Engenharia-Reversa/03-Variaveis`.

**Implementação:** código próprio (`FlowTable`, `VariablesContext`, `ListActions`); sem assemblies PAD.

---

## Formato da tabela de dados

Variáveis de fluxo guardam JSON:

```json
{
  "columns": ["Column1", "Column2"],
  "rows": [["a", "1"], ["b", "2"]]
}
```

Também aceita array de linhas (ex.: saída de `browser_extract_table`) com `first_line_is_header` em leituras CSV.

---

## Paleta (Studio)

Grupo **Variáveis** com subgrupos (`VariablesPaletteCatalog.cs`):

| Subgrupo | Ações |
|----------|-------|
| Variável escalar | 3 (`set_variable`, `increase_variable`, `decrease_variable`) |
| Tabela de dados | 17 (`variables_*`) |
| Números | 2 |
| Listas | 12 (incl. `list_clear`, `list_append` legados) |
| JSON / objeto | 2 |

**Total:** 36 entradas (paridade com mapeamento PAD).

---

## Modelo

- **`VariablesAction`** — operações `variables_*` (31 tipos em `VariablesActionTypes.All`)
- Ações legadas inalteradas: `SetVariableAction`, `IncreaseVariableAction`, `DecreaseVariableAction`, `ListAppendAction`, `ListClearAction`
- **`VariablesEnums`** — modos documentados (CSV, join, filtro, truncar, etc.)
- **`Json/VariablesActionJson.cs`** — campos snake_case compartilhados no painel Studio

Campos comuns: `data_table_variable`, `list_variable`, `output_variable`, `values`, `mode`, `filter_*`, `join_*`, `path`, `separator`, …

---

## Runtime

- **`VariablesContext.Execute`** — dispatch por `action.Operation`
- **`FlowTable`** — parse/serialização, sort, filter, merge, join, CSV, find/replace
- **`ListActions`** — listas JSON (append/clear legados + operações PAD)

### Semântica compartilhada

- **`VariablesSemantics`** — operadores de filtro alinhados ao PAD (`not_contains`, `is_empty`, `begins_with`, …) e aliases do Excel (`eq`, `begins`, …).
- **Merge de tabelas (PAD):** `add_extra_columns`, `ignore_extra_columns`, `error_on_extra_columns` (aliases legados `append_rows` / `union_columns`).
- **CSV:** `mode: delimiter` (padrão) ou `fixed_width` + `column_widths` (`10,20,15` ou JSON array) — equivalente a `ReadCSVWithFixedColumnWidths`.
- **Ordenação de tabela:** `sort_column` + opcional `sort_column2` / `sort_order2`.
- **Find/replace:** `match_case`, `match_entire_cell`, `use_regex`.
- **Listas:** `filter_operator` completo em `variables_filter_list`; remoção por valor remove **a primeira** ocorrência (PAD), ou todas com `remove_all_matches: true`.
- **Filtro de tabela (2 condições):** `filter_column` + opcional `filter_column2` / `filter_operator2` / `filter_value2` / `filter_logic` (`and`|`or`).
- **Filtro de tabela (3+ condições):** `filter_conditions` — array JSON `[{ "column", "operator", "value", "logic"? }, …]`; cada `logic` combina com o resultado acumulado (padrão `and`).
- **Join:** `join_column` (tabela esquerda), `join_column2` (coluna equivalente na direita, se o nome difere), `join_comparison_operator` (`eq` usa índice; `ne`, `gt`, `gte`, `lt`, `lte` comparam chaves linha a linha), `join_operation` (`inner`|`left`|`right`|`full`).
- **Filtrar lista (`variables_filter_list`):** mesmos campos de operador/valor/`filter_conditions` que a tabela (sem `filter_column`); opcional `property_name` para itens JSON.

---

## Studio

- Painel **contextual** por tipo (`VariablesActionUi` + `ShowVarSection*` em `PropertiesPanel.xaml`): tabela, CSV, join/merge, filtros, listas, números, JSON.
- **Template chips** (`TemplateVariableChips`) nos campos de texto que aceitam `{variável}` (filtros, find/replace, path, JSON, etc.).
- Defaults: `VariablesActionDefaults.cs`; paleta: `VariablesPaletteCatalog.cs`.

---

## Demo

Exemplo executável: [`examples/variables-demo-flow`](../../examples/variables-demo-flow) — CSV delimitado e **largura fixa**, merge (`add_extra_columns`), **join** (`join_column2`), filtro **3 condições** (`filter_conditions`), colunas/linhas/clear, listas (reverse/shuffle/remove/clear), escalares, números, JSON, `for_each`; saídas em `output/export.csv` + `output/summary.txt`.

```powershell
hoblio validate examples/variables-demo-flow
hoblio run examples/variables-demo-flow
```

Teste: `DemoFlowTests.Variables_demo_flow_runs`.

### Cobertura do demo vs paleta

| Área | Coberto no demo | Ainda só via testes unitários / outro fluxo |
|------|-----------------|---------------------------------------------|
| Tabela | create, row, modify, add/delete column, delete row, find/replace, dedupe, filter (3 cond.), sort (2 cols), merge, join, read/write CSV, fixed width, column→list, clear | `delete_empty_rows`; find/replace com **regex** / área restrita a coluna |
| Listas | create, append (legado), sort, reverse, shuffle, remove item, filter, merge, common, subtract, clear (legado) | `remove_duplicate_list_items`; `sort_list` com **property_name** (JSON) |
| Números | truncate (round), random int | modos `integer_list` / `list` de aleatórios; truncar `integer_part` / `decimal_part` |
| JSON | json↔object | — |
| Escalares | increase / decrease | `set_variable` (usa painel genérico Assign) |

---

## Pendências e gaps (nov/2026)

Registro do que **não** pretende paridade 1:1 imediata, ou ainda **não feito**, para orientar próximas entregas.

### Runtime / semântica PAD

| Item | Estado | Notas |
|------|--------|--------|
| CSV codificações exóticas (UTF-16, BOM alternativos, cultura) | ○ | Hoje: texto UTF-8; separador custom / fixed width |
| Join `join_comparison_operator` texto (`contains`, `begins_with`, …) | ○ | Suportado: `eq`, `ne`, `gt`, `gte`, `lt`, `lte` |
| Join performance com `ne`/range | ○ | Sem índice; pareamento O(n×m) |
| Filtro tabela: grupos AND/OR aninhados (UI PAD) | ○ | `filter_conditions` é cadeia linear por condição (`logic` entre passos) |
| Find/replace: modos `search_area` / coluna única | Parcial | Campos existem; menos cenários de teste que Excel |
| `variables_sort_list` por propriedade | Parcial | `property_name` + runtime; demo não exercita |
| Truncar número: todos os selectors PAD | Parcial | `round`, `integer`, `truncate`, partes inteira/decimal mapeados em `VariablesSemantics` |
| Objetos custom / tipos .NET PAD | ○ | JSON armazenado como texto parseado; sem CLR types do PAD |

### Validação e telemetria

| Item | Estado | Notas |
|------|--------|--------|
| `VariablesActionValidator` por tipo | ✓ | `GraphValidator` chama em cada nó |
| Testes GraphValidator para **cada** `variables_*` | ○ | Amostra (ex.: join exige `join_column`) |
| Conferência `systemActionName` vs PAD `Actions.log` | ○ | Mapeamento em `VariablesActionTypes.ModuleActionIds`; validação manual pendente (🟡 no ER PAD) |
| Cobertura JSON round-trip 31 tipos | Parcial | `VariablesActionCoverageTests` |

### Studio

| Item | Estado | Notas |
|------|--------|--------|
| Painel contextual Variáveis | ✓ | Seções `ShowVarSection*` |
| Filtro lista dedicado | ✓ | `ShowVarSectionFilterList` |
| Combos/enums visuais (operador, join, merge) | ○ | Campos texto livres + defaults |
| Chips em **todos** os campos template | Parcial | Principais campos; faltam ex. `VarColumnWidths`, sort columns |
| Preview / grid da DataTable | ○ | Fora de escopo |

### Engenharia reversa PAD

Checklist em `PAD-Engenharia-Reversa/03-Variaveis/01-mapeamento-acoes-variaveis.md` (§ G): símbolos Robin exatos, `systemActionName` por fluxo de teste, strings completas do designer UI.

### Verificação local

```powershell
dotnet test Hoblio.sln -c Release
.\scripts\verify-parity.ps1   # inclui validate em examples/variables-demo-flow
```

---

## Checklist ao adicionar ação

1. Constante em `VariablesActionTypes` + `ModuleActionIds`
2. Handler em `VariablesContext` / `ListActions`
3. Entrada em `VariablesPaletteCatalog` + `VariablesActionDefaults`
4. Teste em `VariablesBehaviorTests` ou `VariablesActionCoverageTests`
