# Módulo WebAutomation (Automação do navegador)

Automação **Chrome / Edge** via **CDP** (PuppeteerSharp), alinhada ao grupo **Automação do navegador** do PAD. Referência de engenharia reversa: `E:\Projetos\PAD-Engenharia-Reversa\01-Automacao-Navegador`.

Telemetria: módulo **`WebAutomation`**, `action_id` como no PAD (`LaunchChromeBase`, `GoToWebPageBase`, …).

## Modelo

- Classe única **`WebAutomationAction`** (padrão Word/Excel).
- Discriminante JSON: **`type`** = `browser_*` (29 operações; compatível com fluxos que usavam as 6 ações iniciais).
- Metadados: `WebAutomationActionTypes.ModuleActionIds`.

## Stack vs PAD

| PAD | Elfio |
|-----|--------|
| Extensão + Native Messaging + `LocalWebAutomationRuntime` | PuppeteerSharp + `BrowserContext` |
| Seletores Robin/UIA | **CSS** (`selector`) |
| Firefox / IE | **Não suportado** (Chrome/Edge Chromium) |
| Extrair dados (Live Web Helper) | `browser_extract_table` (tabela HTML → JSON na variável) |
| Se a página contiver (condicional) | Use `browser_wait_content` + nó **If** |
| Obter controle da Web (instância) | Sessão implícita por execução de fluxo (`share_browser` em `run_flow`) |
| Anexar Chrome/Edge (`AttachTo*`) | `browser_attach_chrome` / `browser_attach_edge` + CDP (secção abaixo) |

## Paleta (Studio)

Grupo **Automação do navegador**, três secções como no PAD:

1. Extração de dados da Web  
2. Preenchimento de formulário  
3. Ações gerais  

## Campos JSON comuns

| Campo | Uso |
|-------|-----|
| `url` | Navegação / nova guia / launch opcional |
| `selector` | CSS |
| `timeout_ms` | Esperas DOM, clique, script |
| `wait_ms` | Sleep após navegação |
| `show_on_screen`, `start_maximized` | Launch / `browser_navigate` |
| `text` | Preencher, esperar texto na página |
| `populate_mode` | `replace` (padrão) ou `append` |
| `variable` | Saída (texto, detalhes, script, tabela) |
| `detail` | `url`/`title`/`all` (página) ou `text`/`value`/`href`/… (elemento) |
| `script` | JavaScript (expressão CDP) |
| `path` | Screenshot ou pasta de download |
| `checked` | Checkbox |
| `dropdown_mode`, `dropdown_value` | `by_text` / `by_index` / `by_value` |
| `wait_content_mode` | `contains_text`, `not_contains_text`, `contains_element`, `not_contains_element` |
| `wait_for_navigation`, `wait_selector_after`, `wait_after_timeout_ms` | Pós-clique |
| `wait_visible` | `browser_wait_for` exige visível |
| `browser_engine` | `chrome` ou `edge` (launch) |
| `attach_mode` | `by_url`, `by_title`, `foreground` (anexar) |
| `debugging_port` | Porta CDP (opcional; senão 9222/9223) |

## Anexar a Chrome/Edge já aberto (CDP)

Diferente do PAD (extensão + handle de janela), o Elfio liga-se ao browser via **depuração remota**:

1. Abra o navegador **com** porta de depuração — pelo Studio (**Navegador → Abrir CDP…**), pela CLI ou pelo helper:

   ```powershell
   # mesmo serviço, três portas de entrada:
   dotnet run --project src/Hoblio.Cli -- browser --port 9222 --url https://eproc-1g-sp-hml.tjsp.jus.br/
   pwsh scripts/start-cdp-browser.ps1 -Url https://eproc-1g-sp-hml.tjsp.jus.br/
   ```

   No Studio o diálogo ainda pré-preenche motor e porta a partir do nó `browser_attach_*` selecionado (**Abrir navegador CDP com esta porta…** no inspector).
2. Sem ajuda, chame o executável à mão — **sempre com `--user-data-dir`**:
   - `"C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222 --user-data-dir=%TEMP%\elfio-cdp-profile`
   - `"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --remote-debugging-port=9222 --user-data-dir=%TEMP%\elfio-cdp-profile`
3. No fluxo, use **`browser_attach_chrome`** ou **`browser_attach_edge`** antes dos passos DOM.

**Por que o perfil dedicado:** desde o Chrome/Edge **136** a flag `--remote-debugging-port` é ignorada no perfil padrão (proteção contra roubo de cookies). Além disso, se já existir um Chrome aberto com o perfil padrão, o novo processo apenas entrega a URL a ele — sem porta de debug.

**Serviço:** [`CdpBrowser`](../../src/Hoblio.Runtime/CdpBrowser.cs) — `Launch` (abre + espera o CDP responder, reportando `ReusedRunningBrowser`, abas e PID), `IsListening`, `ListTabs`. É o que o botão do Studio, o verbo `browser` da CLI e o script usam.

| `attach_mode` | Campo | Comportamento |
|---------------|--------|----------------|
| `by_url` | `url` (fragmento) | Primeira aba cuja URL contém o texto |
| `by_title` | `text` (fragmento) | Primeira aba cujo título contém o texto |
| `foreground` | — | Aba que corresponde à janela Chromium **em foco** (Win32); deixe o browser em primeiro plano |

Variáveis de ambiente: **`HOBLIO_BROWSER_DEBUG_PORT`**, **`HOBLIO_BROWSER_DEBUG_URL`** (ex. `http://127.0.0.1:9222`).

**Fim do run / `browser_close`:** sessão **anexada** só desliga o CDP — **não fecha** o browser do usuário. Sessão **lançada** pelo Elfio continua fechando a janela ao terminar.

Quem usa isso hoje: `examples/eproc-peticionamento` no **modo individual** (`if {HoblioRunMode} eq standalone` → attach → caixa de confirmação → guarda de DOM). Ver [composicao-superfluxos.md §13](./composicao-superfluxos.md).

## Ordem típica no fluxo

1. `browser_navigate` ou `browser_launch_chrome` / `browser_launch_edge`  
2. Demais passos na **mesma execução** (sessão CDP partilhada)  
3. `browser_close` opcional (fim do run fecha automaticamente)

## Melhorias nas ações já existentes

- **`browser_fill`**: modo `populate_mode` append (PAD `PopulateTextMode`).  
- **`browser_wait_for`**: opção `wait_visible`.  
- **`browser_navigate`**: JSON e telemetria inalterados; implementação centralizada em `WebAutomationAction`.
