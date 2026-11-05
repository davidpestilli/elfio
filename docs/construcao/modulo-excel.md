# Módulo Excel (`excel_*`)

Automação do **Microsoft Excel** no Windows via **COM** (`Excel.Application`), integrada ao grafo Elfio como ações JSON `excel_*`. Telemetria: módulo **`Excel`**, `action_id` alinhado ao vocabulário de produto (ex.: `LaunchExcel`, `ReadFromExcel`) — ver [catalogo-acoes-e-modulos.md](./catalogo-acoes-e-modulos.md).

**Implementação:** código próprio em `Hoblio.Runtime` (`ExcelContext`, `ExcelComSemantics`); não há cópia de assemblies ou IL de terceiros. Contexto de conformidade: [conformidade/registro-auditoria-independencia-pad.md](../conformidade/registro-auditoria-independencia-pad.md).

---

## Requisitos

- Windows
- Microsoft Excel instalado e registrado (`ProgID` `Excel.Application`)
- Execução local (Studio ou CLI); não há runner headless sem Excel

---

## Paleta (Studio)

Um único grupo **Excel** com **subgrupos** (`PaletteSection`):

| Subgrupo | Ações | Expander padrão |
|----------|-------|-----------------|
| **Básico** | 16 | aberto |
| **Avançado** | 25 | fechado |

Arquivos:

- `src/Hoblio.Studio/ExcelPaletteCatalog.cs` — entradas e aliases de busca (incl. nomes em inglês para filtro)
- `src/Hoblio.Studio/ActionPaletteCatalog.cs` — `PaletteGroup` + `PaletteSection`
- `src/Hoblio.Studio/ActionPalettePanel.xaml` — expander externo (grupo) + `PaletteSubSection` (subgrupo)
- `src/Hoblio.Studio/ViewModels/PaletteGroupViewModel.cs` — `Sections`, contagem no cabeçalho

A busca filtra por subgrupo: só aparecem seções com pelo menos uma ação correspondente; com texto na busca, grupo e seções relevantes expandem.

---

## Modelo (`Hoblio.Model`)

- **`ExcelAction`** — uma classe para todas as operações; discriminant JSON = `type` (`excel_launch`, …)
- **`ExcelActionTypes`** — constantes `excel_*`, lista `All` (41), mapa `ModuleActionIds` → telemetria
- **`ExcelEnums`** — valores documentados para `mode`, filtros, colar, excluir, etc.
- **`ExcelActionMutator` / `ExcelActionPatch`** — clone e rename de variáveis/templates no Studio
- **`Json/ExcelActionJson.cs`** — leitura/escrita snake_case dos campos

### Campos JSON comuns

| Campo | Uso |
|-------|-----|
| `instance` | Variável de fluxo com id da sessão COM (saída em `excel_launch`, entrada nas demais) |
| `worksheet_name` / `worksheet_index` | Planilha alvo (opcional → planilha ativa) |
| `start_row`, `start_column`, `end_row`, `end_column`, `cell_name` | Intervalo ou célula |
| `value` | Escrita, critério simples de filtro, PROCV, append, largura/altura (resize) |
| `path` | Abrir (`launch`) ou Salvar como |
| `output_variable`, `output_variable2` | Saídas (valor, endereço, linha/coluna, listas JSON) |
| `visible`, `read_only`, `save_on_close`, `first_line_is_header` | Flags |
| `mode` | Ordenação (`desc`), shift ao excluir células (`up`/`left`), PROCV (`vlookup`/`hlookup`), resize (`row`/`column`), célula vazia (`down`/`right`) |
| `table_name` | Tabela Excel (`ListObjects`) |

### Campos de paridade avançada

| Campo | Uso |
|-------|-----|
| `target_instance` | Segunda sessão (ex.: copiar planilha para outro workbook) |
| `source_worksheet_name`, `target_worksheet_name`, `worksheet_position` | Cópia de planilha (`after`, `before`, `begin`, `end`) |
| `advanced_filter`, `filter_operator1/2`, `filter_value1/2`, `filter_logic` | AutoFiltro (operadores `eq`, `gt`, `contains`, …; lógica `and`/`or`) |
| `paste_mode` | `all`, `values`, `formulas`, `formats`, `values_formats` |
| `delete_scope` | `cells`, `rows`, `columns` |
| `clear_filter_scope` | `all`, `column`, `table` |
| `sort_column2`, `sort_order2` | Segunda chave de ordenação |
| `match_case`, `match_entire_cell` | Localizar/substituir |
| `match_exact` | PROCV/PROCH exato |

Lista completa de tipos: `ExcelActionTypes.All` em `src/Hoblio.Model/ExcelActionTypes.cs`.

---

## Runtime (`Hoblio.Runtime`)

- **`ExcelContext`** — ciclo de vida da sessão (`Dictionary` id → `Application`), dispatch por `action.Type`
- **`ComActiveObject`** — anexar a Excel já aberto (`oleaut32` `GetActiveObject`)
- **`ExcelComSemantics`** — critérios de filtro, `PasteSpecial`, operadores lógicos

Fluxo de sessão:

1. `excel_launch` ou `excel_attach` grava em `instance` um guid interno
2. Demais ações resolvem a sessão pela variável `instance`
3. `excel_close` encerra `Application` e remove a variável

**Saídas em lista:** `excel_get_all_worksheets` grava JSON array em `output_variable` (compatível com `for_each`).

### Ações implementadas no runtime

Todas as **41** entradas da paleta têm handler; algumas operações são **simplificadas** face ao designer completo de mercado (ex.: filtros multi-coluna complexos, copiar planilha entre dois processos Excel distintos).

---

## Studio — propriedades

Painel único **Excel** (`FlowEditorViewModel.ExcelFields.cs` + `PropertiesPanel.xaml`): campos compartilhados acima; preencha só o que a ação usa.

Defaults por tipo: `ExcelActionDefaults.cs`. Inserção na paleta: `ActionDefaults.Create` → `ExcelActionDefaults.TryCreate`.

---

## Manifest

Projetos que usam Excel passam a incluir `module_references` com `"name": "Excel"` após `validate --update-manifest` ou save no Studio.

---

## Exemplo

**`examples/excel-demo-flow`**

Sequência: Launch → gravar tabela Produto/Qtd → filtrar `Banana` → ler célula → log → SaveAs `output/excel-demo.xlsx` → Close.

```powershell
dotnet run --project src/Hoblio.Cli -- validate examples/excel-demo-flow
dotnet run --project src/Hoblio.Cli -- run examples/excel-demo-flow --jsonl
```

Abrir a pasta no Studio ou editar `flow.json` manualmente.

---

## Testes

| Teste | O quê |
|-------|--------|
| `ExcelActionCoverageTests` | 41 tipos + `ModuleActionIds` |
| `ActionKindTests.Excel_*` | Round-trip JSON + catálogo |
| `PaletteCatalogStructureTests` | 1 grupo Excel, 2 seções, 16+25 itens |

```powershell
dotnet test --filter "Excel|PaletteCatalog"
```

---

## Adicionar ou alterar uma ação Excel

1. Constante + `ModuleActionIds` em `ExcelActionTypes.cs`
2. Entrada em `ExcelPaletteCatalog` (Básico ou Avançado)
3. `ExcelActionJson` se novos campos; `ExcelActionDefaults` para inserção na paleta
4. Ramo em `ExcelContext.Execute`
5. `ActionCatalog.Describe` (automático via `ExcelActionTypes.ModuleActionId`)
6. `ActionExecutor` (automático via `ExcelAction`)
7. Campos no painel Excel, se necessário
8. Teste de round-trip JSON
9. Linha neste doc e em [catalogo-acoes-e-modulos.md](./catalogo-acoes-e-modulos.md)
