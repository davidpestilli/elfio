# Catálogo de ações e módulos lógicos

## Objetivo

Todo passo registrado em log, telemetria ou integração externa deve usar **dois identificadores estáveis**:

| Campo | Papel | Exemplo |
|-------|--------|---------|
| **`module`** | Agrupamento lógico (paleta / capacidade) | `WebAutomation`, `Variables`, `Loops` |
| **`action_id`** | Nome curto da operação dentro do módulo | `LaunchChrome`, `SetVariable`, `Foreach` |

No JSON do fluxo continua valendo o discriminant **`type`** (`browser_navigate`, `set_variable`, …). `module` + `action_id` não substituem `type`; servem para **logs, manifest de capacidades e plugins futuros** sem renomear projetos salvos.

Convenções:

- **`module`**: PascalCase, mesmo vocabulário dos grupos de produto (Browser → `WebAutomation` para ações de navegador CDP; HTTP utilitário → `Web`).
- **`action_id`**: PascalCase; sufixo `Base` quando a operação for variante extensível (alinhado a nomes de telemetria de mercado).
- **Controles de fluxo** usam módulos **virtuais** (`Conditionals`, `Loops`, `FlowControl`) — não correspondem a assembly, apenas a construtos do interpretador.

---

## Tabela — ações (`ActionKind`)

| `type` (JSON) | Paleta Studio | `module` | `action_id` | Variáveis produzidas |
|---------------|---------------|----------|-------------|----------------------|
| `log` | Utilitários | `System` | `LogMessage` | — |
| `no_op` | Utilitários | `System` | `NoOp` | — |
| `delay` | Utilitários | `System` | `Wait` | — |
| `http_get` | Utilitários | `Web` | `InvokeWebServiceBase` | `variable` (opcional) |
| `show_message` | Caixa de mensagens | `Display` | `ShowMessage` | — |
| `show_get_user_input` | Caixa de mensagens | `Display` | `DisplayInputDialog` | `user_input_variable`, `button_pressed_variable` |
| `show_select_from_list` | Caixa de mensagens | `Display` | `SelectFromList` | `selected_item_variable`, `selected_index_variable`, `button_pressed_variable` |
| `file_write` | Arquivos | `File` | `WriteText` | — |
| `file_append` | Arquivos | `File` | `AppendLine` | — |
| `file_read` | Arquivos | `File` | `ReadText` | `variable` |
| `browser_*` (29 tipos) | Automação do navegador | `WebAutomation` | `LaunchChromeBase`, `AttachToChromeByUrl`, … | ver [modulo-web-automation.md](./modulo-web-automation.md) |
| `set_variable` | Variáveis | `Variables` | `Assign` | `variable` |
| `increase_variable` | Variáveis | `Variables` | `IncreaseVariable` | — |
| `decrease_variable` | Variáveis | `Variables` | `DecreaseVariable` | — |
| `list_append` | Variáveis | `Variables` | `AddItemToList` | — |
| `list_clear` | Variáveis | `Variables` | `ClearList` | — |
| `run_flow` | Fluxos | `FlowControl` | `CallFunction` | `outputs[]` |
| `excel_*` (41 tipos) | Excel → Básico / Avançado | `Excel` | `LaunchExcel`, `ReadFromExcel`, … | ver `ExcelActionTypes` |
| `word_*` (8 tipos) | Word | `Word` | `LaunchWord`, `ReadFromWord`, … | ver `WordActionTypes` |

Notas:

- **`Web` vs `WebAutomation`**: `http_get` é requisição HTTP genérica; ações `browser_*` compartilham sessão CDP (`BrowserContext`).
- **`Variables` / `Assign`**: atribuição é ação no grafo; listas vivem em `RunVariables` como JSON array serializado.
- **`run_flow`**: `action_id` `CallFunction`; `flow_ref` no log deve ser `flow_id`, caminho relativo ou rótulo do subfluxo embutido.

---

## Tabela — controles (`ControlKind` / nós de fluxo)

| `kind` (nó) | Paleta | `module` | `action_id` | Log resumido (hoje) |
|-------------|--------|----------|-------------|---------------------|
| `if` | Condicionais | `Conditionals` | `If` | `if … => then\|else` |
| `switch` | Condicionais | `Conditionals` | `Switch` | `switch {expr} => \`value\`` |
| `case` | Condicionais | `Conditionals` | `Case` | `case … => then\|else` |
| `case_default` | Condicionais | `Conditionals` | `CaseDefault` | `case default` |
| `for_each` | Loops | `Loops` | `Foreach` | `for_each \`list\` -> port …` |
| `while_loop` | Loops | `Loops` | `While` | `while … => …` |
| `repeat` | Loops | `Loops` | `Loop` | `repeat \`N\` -> port …` |
| `next_loop` | Loops | `Loops` | `Continue` | `next_loop` |
| `exit_loop` | Loops | `Loops` | `Break` | `exit_loop` |
| `start` | Utilitários | `FlowControl` | `Start` | (sem `ActionExecuted`) |
| `end` | Utilitários | `FlowControl` | `End` | (sem `ActionExecuted`; término do grafo) |

---

## Infraestrutura de erro (telemetria)

Quando retry ou `on_failure: continue` trata falha **sem** ramo On error no grafo, registrar linha extra ou metadados:

| Situação | `module` | `action_id` | Campo |
|----------|----------|-------------|--------|
| Repetição após falha | *(null)* | `RepeatAction` | `inside_error_handling: true` |

Entre tentativas de retry, o runtime emite `action_executed` com `action_id: RepeatAction`, `module` ausente e `inside_error_handling: true` (ver [telemetria-e-logs.md](./telemetria-e-logs.md)).

---

## Onde usar o catálogo

| Consumidor | Uso |
|------------|-----|
| **Runtime** | Preencher `module` / `action_id` em `RunEvent.ActionExecuted` e JSONL |
| **Validação** | Derivar `module_references` no manifest (ver [manifest-e-capacidades.md](./manifest-e-capacidades.md)) |
| **Studio** | Rótulos de paleta permanecem amigáveis; IDs estáveis ficam ocultos ou em coluna avançada |
| **Testes** | Asserções sobre `action_id` em vez de substring do `summary` |

---

## Evolução — descritor único (não implementado)

Objetivo: uma tabela ou fonte gerada (`ActionDescriptor`) com:

```json
{
  "type": "browser_navigate",
  "module": "WebAutomation",
  "action_id": "LaunchChromeBase",
  "display_name": "Browser navigate",
  "palette_group": "Browser",
  "arguments": [
    { "name": "url", "required": true },
    { "name": "wait_ms", "required": false }
  ]
}
```

`ActionExecutor`, validador e Studio leem o mesmo descritor — equivalente funcional a metadados de ação em catálogo central, sem alterar o JSON salvo dos fluxos.

---

## Adicionar nova ação (checklist)

1. Classe em `Hoblio.Model` + entrada em `ActionKindJsonConverter`.
2. Ramo em `ActionExecutor` + testes em `DemoFlowTests` ou unitários.
3. Entrada na paleta (`ActionPaletteCatalog`) e painel de propriedades no Studio.
4. Linha nesta tabela (`module`, `action_id`).
5. Atualizar derivador de `module_references` quando existir no manifest.
