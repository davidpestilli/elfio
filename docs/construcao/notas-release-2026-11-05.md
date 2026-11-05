# Notas de release — 2026-11-05

Registro do que entrou neste commit: nova ação de diálogo, exemplo eproc, módulo Excel, ajustes de Studio e documentação.

---

## 1. Ação `show_select_from_list` (Caixa de mensagens)

Diálogo nativo Windows (WPF, thread STA) para escolher um ou mais itens de uma lista.

| Campo JSON | Descrição |
|------------|-----------|
| `title`, `message` | Cabeçalho e texto do diálogo; aceitam `{variavel}` |
| `list` | Itens **um por linha** ou array JSON após interpolação |
| `topmost` | Manter janela sempre no topo |
| `limit_to_list` | Se `true`, só valores da lista (combo fechado); se `false`, combo editável |
| `allow_empty_selection` | Permite OK sem seleção |
| `allow_multiple_selections` | ListBox com seleção múltipla; saídas em JSON array |
| `selected_item_variable` | Padrão `SelectedItem` |
| `selected_index_variable` | Padrão `SelectedIndex` (número ou `-1`; múltipla → array JSON) |
| `button_pressed_variable` | Padrão `ButtonPressed` (`OK` / `Cancel`) |

**Telemetria / catálogo:** módulo `Display`, `action_id` `SelectFromList`.

**Implementação:**

- Modelo: `ShowSelectFromListAction` em `ActionKind.cs`
- Runtime: `SelectFromListDialog.xaml`, `NativeDialogs.RunShowSelectFromList`, `SelectFromListItems.Parse`
- Studio: paleta **Escolher da lista**, painel Properties, produtores de variável, defaults e testes JSON

Comportamento alinhado às outras ações Display: bloqueia o fluxo até OK/Cancel durante **Run** (Windows).

---

## 2. Exemplo `examples/login-eproc`

### Subfluxo `pick_environment`

- Nó **Escolhe ambiente** passou de `show_get_user_input` para **`show_select_from_list`**
- Lista fixa: `hml` / `prd`
- **`EprocEnvChoice`** e o switch `{EprocEnvChoice}` (case `prd` / default homologação) permanecem iguais
- Novas saídas opcionais: `EprocEnvIndex`, `EprocEnvButton`

### Subfluxo `detect_screen`

Três probes **`browser_get_text`** em série (login → OTP → painel), todos com **`timeout_ms`: 1000** e **`on_failure`: `continue`**.

| Cenário | Tempo aproximado por execução do subfluxo |
|---------|---------------------------------------------|
| Nenhum seletor encontrado | até ~3 s (3 × 1 s) |
| Uma tela reconhecida | até ~2 s (dois timeouts + um sucesso rápido) |

O subfluxo **não para** no primeiro probe com sucesso; `ScreenText` fica com o valor do **último** probe que gravou texto com sucesso. Cada chamada a `run_flow` → `detect_screen` no fluxo principal repete os três probes.

---

## 3. Módulo Excel (`excel_*`)

- **41** tipos de ação COM (`ExcelActionTypes`), paleta Studio (Básico / Avançado), runtime `ExcelContext` / `ExcelComSemantics`
- Exemplo: `examples/excel-demo-flow`
- Requisito: Microsoft Excel instalado (Windows)
- Testes: `ExcelActionCoverageTests`, `PaletteCatalogStructureTests`

Ver linha `excel_*` em [catalogo-acoes-e-modulos.md](catalogo-acoes-e-modulos.md).

---

## 4. Studio — UI e log

Documentado em [studio-ui-paleta-log.md](studio-ui-paleta-log.md): paleta (busca, cards, tema), sidebars fechados por padrão, log de execução com foco no nó (`FlashNode` no canvas).

---

## 5. Verificação

```powershell
dotnet test
dotnet run --project src/Hoblio.Cli -- validate examples/login-eproc
dotnet run --project src/Hoblio.Cli -- validate examples/excel-demo-flow
```

Paridade atualizada em [PARITY.md](../PARITY.md).
